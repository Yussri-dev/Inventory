using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.LocalDB.Services
{
    public sealed class LocalStoreProfileService
        : ILocalStoreProfileService
    {
        private static readonly HashSet<string>
            AllowedLogoContentTypes =
                new(
                    StringComparer.OrdinalIgnoreCase)
                {
                    "image/png",
                    "image/jpeg",
                    "image/jpg",
                    "image/webp"
                };

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;
        private readonly ReceiptSettings _receiptSettings;
        private readonly ILogger<LocalStoreProfileService> _logger;

        public LocalStoreProfileService(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext,
            IOptions<ReceiptSettings> receiptSettings,
            ILogger<LocalStoreProfileService> logger)
        {
            _db =
                db;

            _tenantContext =
                tenantContext;

            _receiptSettings =
                receiptSettings.Value;

            _logger =
                logger;
        }

        public async Task UpsertAsync(
            LocalStoreProfile profile,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                profile);

            var currentTenantId =
                _tenantContext.GetRequiredTenantId();

            if (profile.TenantId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The store profile tenant id is required.");
            }

            if (profile.TenantId != currentTenantId)
            {
                throw new InvalidOperationException(
                    "The store profile belongs to another tenant.");
            }

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                _db.ChangeTracker.Clear();

                var local =
                    await _db.StoreProfiles
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == currentTenantId,
                            cancellationToken);

                var isNewProfile =
                    local == null;

                if (local == null)
                {
                    local =
                        new LocalStoreProfile
                        {
                            TenantId =
                                currentTenantId
                        };

                    _db.StoreProfiles.Add(
                        local);
                }

                /*
                 * Une configuration locale du ticket a priorité sur
                 * les valeurs reçues pendant la synchronisation.
                 */
                var preserveLocalReceiptConfiguration =
                    !isNewProfile &&
                    local.ReceiptConfigurationUpdatedAtUtc.HasValue;

                ApplyGeneralStoreProfile(
                    local,
                    profile);

                if (!preserveLocalReceiptConfiguration)
                {
                    ApplyIncomingReceiptConfiguration(
                        local,
                        profile);
                }

                local.LastSyncedAtUtc =
                    DateTime.UtcNow;

                await _db.SaveChangesAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Local store profile saved for tenant {TenantId}. " +
                    "ReceiptConfigurationPreserved={ReceiptConfigurationPreserved}.",
                    currentTenantId,
                    preserveLocalReceiptConfiguration);
            }
            finally
            {
                _db.ChangeTracker.Clear();

                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        public async Task<LocalStoreProfile?> GetCurrentAsync(
            CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            return await _db.StoreProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId,
                    cancellationToken);
        }

        public async Task<LocalStoreProfile>
            UpdateReceiptConfigurationAsync(
                UpdateLocalReceiptConfigurationRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ValidateReceiptConfiguration(
                request);

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            await LocalDatabaseWriteGate.Semaphore.WaitAsync(
                cancellationToken);

            try
            {
                _db.ChangeTracker.Clear();

                var profile =
                    await _db.StoreProfiles
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId,
                            cancellationToken);

                if (profile == null)
                {
                    throw new InvalidOperationException(
                        "The local store profile is missing. " +
                        "Synchronize the tenant profile before configuring " +
                        "the receipt.");
                }

                profile.ReceiptCurrencyCode =
                    NormalizeCurrencyCode(
                        request.CurrencyCode);

                profile.ReceiptHeaderTagLine =
                    NormalizeOptional(
                        request.HeaderTagLine);

                profile.ReceiptHeader =
                    NormalizeOptional(
                        request.ReceiptHeader);

                profile.ReceiptSocialLine =
                    NormalizeOptional(
                        request.SocialLine);

                profile.ReceiptExtraAddressLine =
                    NormalizeOptional(
                        request.ExtraAddressLine);

                profile.ReceiptFooter =
                    NormalizeOptional(
                        request.ReceiptFooter);

                profile.ReceiptDefaultCashierName =
                    NormalizeOptional(
                        request.DefaultCashierName);

                if (request.RemoveLogo)
                {
                    profile.ReceiptLogoBytes =
                        null;

                    profile.ReceiptLogoFileName =
                        null;

                    profile.ReceiptLogoContentType =
                        null;
                }
                else if (request.LogoBytes is { Length: > 0 })
                {
                    profile.ReceiptLogoBytes =
                        request.LogoBytes.ToArray();

                    profile.ReceiptLogoFileName =
                        NormalizeOptional(
                            request.LogoFileName);

                    profile.ReceiptLogoContentType =
                        NormalizeOptional(
                            request.LogoContentType);
                }

                /*
                 * Cette date indique qu'un utilisateur a personnalisé
                 * le ticket localement.
                 *
                 * UpsertAsync préservera alors ces valeurs pendant
                 * les synchronisations suivantes.
                 */
                profile.ReceiptConfigurationUpdatedAtUtc =
                    DateTime.UtcNow;

                await _db.SaveChangesAsync(
                    cancellationToken);

                _db.Entry(profile).State =
                    EntityState.Detached;

                _logger.LogInformation(
                    "Receipt configuration updated for tenant {TenantId}. " +
                    "LogoConfigured={LogoConfigured}.",
                    tenantId,
                    profile.ReceiptLogoBytes is { Length: > 0 });

                return profile;
            }
            finally
            {
                _db.ChangeTracker.Clear();

                LocalDatabaseWriteGate.Semaphore.Release();
            }
        }

        private static void ApplyGeneralStoreProfile(
            LocalStoreProfile local,
            LocalStoreProfile incoming)
        {
            local.Name =
                NormalizeRequired(
                    incoming.Name,
                    "Store name");

            local.LegalName =
                NormalizeOptional(
                    incoming.LegalName);

            local.TradeName =
                NormalizeOptional(
                    incoming.TradeName);

            local.TaxNumber =
                NormalizeOptional(
                    incoming.TaxNumber);

            local.RegistrationNumber =
                NormalizeOptional(
                    incoming.RegistrationNumber);

            local.Address =
                NormalizeOptional(
                    incoming.Address);

            local.City =
                NormalizeOptional(
                    incoming.City);

            local.State =
                NormalizeOptional(
                    incoming.State);

            local.PostalCode =
                NormalizeOptional(
                    incoming.PostalCode);

            local.Country =
                NormalizeOptional(
                    incoming.Country);

            local.Phone =
                NormalizeOptional(
                    incoming.Phone);

            local.Mobile =
                NormalizeOptional(
                    incoming.Mobile);

            local.Email =
                NormalizeOptional(
                    incoming.Email);

            local.Website =
                NormalizeOptional(
                    incoming.Website);

            local.LogoUrl =
                NormalizeOptional(
                    incoming.LogoUrl);

            local.Currency =
                string.IsNullOrWhiteSpace(
                    incoming.Currency)
                        ? "EUR"
                        : incoming.Currency
                            .Trim()
                            .ToUpperInvariant();

            local.CurrencySymbol =
                string.IsNullOrWhiteSpace(
                    incoming.CurrencySymbol)
                        ? "€"
                        : incoming.CurrencySymbol.Trim();

            local.Locale =
                string.IsNullOrWhiteSpace(
                    incoming.Locale)
                        ? "fr-BE"
                        : incoming.Locale.Trim();
        }

        private static void ApplyIncomingReceiptConfiguration(
            LocalStoreProfile local,
            LocalStoreProfile incoming)
        {
            local.ReceiptHeader =
                NormalizeOptional(
                    incoming.ReceiptHeader);

            local.ReceiptFooter =
                NormalizeOptional(
                    incoming.ReceiptFooter);

            local.ReceiptCurrencyCode =
                FirstNotEmpty(
                    incoming.ReceiptCurrencyCode,
                    incoming.Currency,
                    "EUR")
                .ToUpperInvariant();

            local.ReceiptHeaderTagLine =
                NormalizeOptional(
                    incoming.ReceiptHeaderTagLine);

            local.ReceiptSocialLine =
                NormalizeOptional(
                    incoming.ReceiptSocialLine);

            local.ReceiptExtraAddressLine =
                NormalizeOptional(
                    incoming.ReceiptExtraAddressLine);

            local.ReceiptDefaultCashierName =
                NormalizeOptional(
                    incoming.ReceiptDefaultCashierName);

            if (incoming.ReceiptLogoBytes is { Length: > 0 })
            {
                local.ReceiptLogoBytes =
                    incoming.ReceiptLogoBytes.ToArray();

                local.ReceiptLogoFileName =
                    NormalizeOptional(
                        incoming.ReceiptLogoFileName);

                local.ReceiptLogoContentType =
                    NormalizeOptional(
                        incoming.ReceiptLogoContentType);
            }

            local.ReceiptConfigurationUpdatedAtUtc =
                incoming.ReceiptConfigurationUpdatedAtUtc;
        }

        private void ValidateReceiptConfiguration(
            UpdateLocalReceiptConfigurationRequest request)
        {
            if (string.IsNullOrWhiteSpace(
                    request.CurrencyCode))
            {
                throw new InvalidOperationException(
                    "Currency code is required.");
            }

            ValidateMaximumLength(
                request.CurrencyCode,
                10,
                "Currency code");

            ValidateMaximumLength(
                request.HeaderTagLine,
                200,
                "Header tag line");

            ValidateMaximumLength(
                request.ReceiptHeader,
                2000,
                "Receipt header");

            ValidateMaximumLength(
                request.SocialLine,
                200,
                "Social line");

            ValidateMaximumLength(
                request.ExtraAddressLine,
                300,
                "Extra address line");

            ValidateMaximumLength(
                request.ReceiptFooter,
                2000,
                "Receipt footer");

            ValidateMaximumLength(
                request.DefaultCashierName,
                100,
                "Default cashier name");

            ValidateMaximumLength(
                request.LogoFileName,
                200,
                "Logo file name");

            ValidateMaximumLength(
                request.LogoContentType,
                100,
                "Logo content type");

            var maximumLogoSize =
                _receiptSettings.MaximumLogoSizeBytes > 0
                    ? _receiptSettings.MaximumLogoSizeBytes
                    : 1_048_576;

            if (request.LogoBytes is
                {
                    Length: > 0
                } &&
                request.LogoBytes.Length > maximumLogoSize)
            {
                throw new InvalidOperationException(
                    $"The receipt logo cannot exceed " +
                    $"{maximumLogoSize:N0} bytes.");
            }

            if (request.LogoBytes is { Length: > 0 } &&
                !string.IsNullOrWhiteSpace(
                    request.LogoContentType) &&
                !AllowedLogoContentTypes.Contains(
                    request.LogoContentType.Trim()))
            {
                throw new InvalidOperationException(
                    "Only PNG, JPEG and WebP receipt logos are supported.");
            }
        }

        private static void ValidateMaximumLength(
            string? value,
            int maximumLength,
            string propertyName)
        {
            if (!string.IsNullOrWhiteSpace(value) &&
                value.Trim().Length > maximumLength)
            {
                throw new InvalidOperationException(
                    $"{propertyName} cannot exceed " +
                    $"{maximumLength} characters.");
            }
        }

        private static string NormalizeCurrencyCode(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "EUR"
                : value
                    .Trim()
                    .ToUpperInvariant();
        }

        private static string NormalizeRequired(
            string? value,
            string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{propertyName} is required.");
            }

            return value.Trim();
        }

        private static string? NormalizeOptional(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string FirstNotEmpty(
            params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }
    }
}