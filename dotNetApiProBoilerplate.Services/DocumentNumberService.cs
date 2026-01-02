using Inventory.Domain.Models;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services
{
    public class DocumentNumberService : IDocumentNumberService
    {
        private readonly IRepository<DocumentNumber> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DocumentNumberService(
            IRepository<DocumentNumber> repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateAsync(string documentType)
        {
            var now = DateTime.UtcNow;

            // Load configuration for this document type
            var config = await _repository
                .GetSingleAsync(d =>
                    d.DocumentType == documentType &&
                    d.Year == now.Year &&
                    (!d.ResetMonthly || d.Month == now.Month) &&
                    !d.IsDeleted);

            if (config == null)
            {
                config = new DocumentNumber
                {
                    Id = Guid.NewGuid(),
                    DocumentType = documentType,
                    Prefix = documentType.ToUpper(),
                    LastNumber = 0,
                    PaddingLength = 6,
                    Year = now.Year,
                    Month = now.Month,
                    ResetYearly = true,
                    ResetMonthly = false,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(config);
            }
            else
            {
                // Reset rules
                if (config.ResetYearly && config.Year != now.Year)
                {
                    config.Year = now.Year;
                    config.LastNumber = 0;
                }

                if (config.ResetMonthly && config.Month != now.Month)
                {
                    config.Month = now.Month;
                    config.LastNumber = 0;
                }

                config.ModifiedAt = DateTime.UtcNow;
            }

            config.LastNumber++;

            var numberPart = config.LastNumber
                .ToString()
                .PadLeft(config.PaddingLength, '0');

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(config.Prefix))
                parts.Add(config.Prefix);

            if (config.ResetYearly)
                parts.Add(now.Year.ToString());

            if (config.ResetMonthly)
                parts.Add(now.Month.ToString("00"));

            parts.Add(numberPart);

            if (!string.IsNullOrWhiteSpace(config.Suffix))
                parts.Add(config.Suffix);

            await _unitOfWork.SaveChangesAsync();

            return string.Join("-", parts);
        }
    }
}
