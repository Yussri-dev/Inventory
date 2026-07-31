using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Microsoft.Extensions.Logging;


namespace Inventory.Ui.Services.Sync
{
    public sealed class TenantStoreProfileSyncService
    : ITenantStoreProfileSyncService
    {
        private readonly ITenantApi _tenantApi;
        private readonly ILocalStoreProfileService _localStoreProfileService;
        private readonly ILocalTenantContext _tenantContext;
        private readonly ILogger<TenantStoreProfileSyncService> _logger;

        public TenantStoreProfileSyncService(
            ITenantApi tenantApi,
            ILocalStoreProfileService localStoreProfileService,
            ILocalTenantContext tenantContext,
            ILogger<TenantStoreProfileSyncService> logger)
        {
            _tenantApi = tenantApi;
            _localStoreProfileService = localStoreProfileService;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task SynchronizeAsync(
            CancellationToken cancellationToken = default)
        {
            var currentTenantId =
                _tenantContext.GetRequiredTenantId();

            /*
             * L'appel HTTP reste dans Inventory.Ui.
             */
            var serverTenant =
                await _tenantApi.GetMyTenantAsync(
                    cancellationToken);

            if (serverTenant == null)
            {
                throw new InvalidOperationException(
                    "The tenant API returned an empty response.");
            }

            if (serverTenant.Id != currentTenantId)
            {
                throw new InvalidOperationException(
                    "The downloaded store profile belongs to another tenant.");
            }

            var localProfile =
                new LocalStoreProfile
                {
                    TenantId =
                        serverTenant.Id,

                    Name =
                        serverTenant.Name,

                    LegalName =
                        serverTenant.LegalName,

                    TradeName =
                        serverTenant.TradeName,

                    TaxNumber =
                        serverTenant.TaxNumber,

                    RegistrationNumber =
                        serverTenant.RegistrationNumber,

                    Address =
                        serverTenant.Address,

                    City =
                        serverTenant.City,

                    State =
                        serverTenant.State,

                    PostalCode =
                        serverTenant.PostalCode,

                    Country =
                        serverTenant.Country,

                    Phone =
                        serverTenant.Phone,

                    Mobile =
                        serverTenant.Mobile,

                    Email =
                        serverTenant.Email,

                    Website =
                        serverTenant.Website,

                    LogoUrl =
                        serverTenant.LogoUrl,

                    ReceiptHeader =
                        serverTenant.ReceiptHeader,

                    ReceiptFooter =
                        serverTenant.ReceiptFooter,

                    Currency =
                        string.IsNullOrWhiteSpace(
                            serverTenant.Currency)
                            ? "EUR"
                            : serverTenant.Currency,

                    CurrencySymbol =
                        string.IsNullOrWhiteSpace(
                            serverTenant.CurrencySymbol)
                            ? "€"
                            : serverTenant.CurrencySymbol,

                    Locale =
                        string.IsNullOrWhiteSpace(
                            serverTenant.Locale)
                            ? "fr-BE"
                            : serverTenant.Locale,

                    LastSyncedAtUtc =
                        DateTime.UtcNow
                };

            await _localStoreProfileService.UpsertAsync(
                localProfile,
                cancellationToken);

            _logger.LogInformation(
                "Tenant store profile synchronized for tenant {TenantId}.",
                currentTenantId);
        }
    }
}
