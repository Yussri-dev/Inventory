using Inventory.Domain.Models;
using Inventory.Infrastructure.Repositories;
using Inventory.Services.Abstractions;
using Inventory.Services.Context;

namespace Inventory.Services
{
    public class DocumentNumberService : IDocumentNumberService
    {
        private readonly IRepository<DocumentNumber> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantContext _tenantContext;

        public DocumentNumberService(
            IRepository<DocumentNumber> repository,
            IUnitOfWork unitOfWork,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
        }

        public async Task<string> GenerateAsync(
    string documentType)
        {
            var numbers =
                await GenerateBatchTrackedAsync(
                    documentType,
                    1);

            await _unitOfWork.SaveChangesAsync();

            return numbers[0];
        }

        public async Task<IReadOnlyList<string>>
            GenerateBatchTrackedAsync(
                string documentType,
                int count)
        {
            if (string.IsNullOrWhiteSpace(
                    documentType))
            {
                throw new ArgumentException(
                    "Document type is required.",
                    nameof(documentType));
            }

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "Document count must be greater than zero.");
            }

            var tenantId =
                _tenantContext.TenantId;

            var userId =
                _tenantContext.UserId;

            var now =
                DateTime.UtcNow;

            var config =
                await _repository.GetSingleAsync(
                    document =>
                        document.DocumentType == documentType &&
                        document.TenantId == tenantId &&
                        document.Year == now.Year &&
                        (
                            !document.ResetMonthly ||
                            document.Month == now.Month
                        ) &&
                        !document.IsDeleted);

            if (config == null)
            {
                config =
                    new DocumentNumber
                    {
                        Id =
                            Guid.NewGuid(),

                        DocumentType =
                            documentType,

                        TenantId =
                            tenantId,

                        CreatedByUserId =
                            userId,

                        Prefix =
                            documentType.ToUpperInvariant(),

                        LastNumber =
                            0,

                        PaddingLength =
                            6,

                        Year =
                            now.Year,

                        Month =
                            now.Month,

                        ResetYearly =
                            true,

                        ResetMonthly =
                            false,

                        CreatedAt =
                            now
                    };

                await _repository.AddAsync(
                    config);
            }
            else
            {
                if (config.ResetYearly &&
                    config.Year != now.Year)
                {
                    config.Year =
                        now.Year;

                    config.LastNumber =
                        0;
                }

                if (config.ResetMonthly &&
                    config.Month != now.Month)
                {
                    config.Month =
                        now.Month;

                    config.LastNumber =
                        0;
                }

                config.ModifiedAt =
                    now;

                config.ModifiedByUserId =
                    userId;
            }

            var numbers =
                new List<string>(
                    count);

            for (var index = 0;
                 index < count;
                 index++)
            {
                config.LastNumber++;

                var numberPart =
                    config.LastNumber
                        .ToString()
                        .PadLeft(
                            config.PaddingLength,
                            '0');

                var parts =
                    new List<string>();

                if (!string.IsNullOrWhiteSpace(
                        config.Prefix))
                {
                    parts.Add(
                        config.Prefix);
                }

                if (config.ResetYearly)
                {
                    parts.Add(
                        now.Year.ToString());
                }

                if (config.ResetMonthly)
                {
                    parts.Add(
                        now.Month.ToString("00"));
                }

                parts.Add(
                    numberPart);

                if (!string.IsNullOrWhiteSpace(
                        config.Suffix))
                {
                    parts.Add(
                        config.Suffix);
                }

               
                numbers.Add(
                    string.Join(
                        "0",
                        parts));
            }

           
            return numbers;
        }
    }
}