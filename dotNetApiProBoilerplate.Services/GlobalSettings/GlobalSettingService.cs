using Inventory.Domain.Models;
using Inventory.Dto.GlobalRequests.Results;
using Inventory.Infrastructure.Data;
using Inventory.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.GlobalSettings
{
    public sealed class GlobalSettingService
    : IGlobalSettingService
    {
        private readonly InventoryDbContext _context;

        public GlobalSettingService(
            InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<GlobalConfigurationResult> GetAsync(
            CancellationToken cancellationToken = default)
        {
            var setting =
                await GetOrCreateEntityAsync(
                    cancellationToken);

            return Map(setting);
        }

        public async Task<GlobalConfigurationResult> UpdateAsync(
            UpdateGlobalConfigurationRequest request,
            Guid? updatedByUserId,
            CancellationToken cancellationToken = default)
        {
            var setting =
                await GetOrCreateEntityAsync(
                    cancellationToken);

            setting.DefaultVatRate =
                request.DefaultVatRate;

            setting.DefaultCurrency =
                request.DefaultCurrency.Trim().ToUpperInvariant();

            setting.DefaultLocale =
                request.DefaultLocale.Trim();

            setting.DefaultTimeZone =
                request.DefaultTimeZone.Trim();

            setting.UpdatedAtUtc =
                DateTime.UtcNow;

            setting.UpdatedByUserId =
                updatedByUserId;

            await _context.SaveChangesAsync(
                cancellationToken);

            return Map(setting);
        }

        private async Task<GlobalSetting> GetOrCreateEntityAsync(
            CancellationToken cancellationToken)
        {
            var setting =
                await _context.GlobalSettings
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (setting is not null)
            {
                return setting;
            }

            setting =
                new GlobalSetting
                {
                    DefaultVatRate = 21m,
                    DefaultCurrency = "EUR",
                    DefaultLocale = "fr-BE",
                    DefaultTimeZone = "Europe/Brussels",
                    CreatedAtUtc = DateTime.UtcNow
                };

            await _context.GlobalSettings.AddAsync(
                setting,
                cancellationToken);

            await _context.SaveChangesAsync(
                cancellationToken);

            return setting;
        }

        private static GlobalConfigurationResult Map(
            GlobalSetting setting)
        {
            return new GlobalConfigurationResult
            {
                DefaultVatRate =
                    setting.DefaultVatRate,

                DefaultCurrency =
                    setting.DefaultCurrency,

                DefaultLocale =
                    setting.DefaultLocale,

                DefaultTimeZone =
                    setting.DefaultTimeZone
            };
        }
    }
}
