using Inventory.Dto.CashReports.Results;
using MediatR;

namespace Inventory.Services.Features.CashReport.GetById
{
    public class GetCashReportByIdQuery : IRequest<CashReportResult>
    {
        public Guid Id { get; }

        public GetCashReportByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
