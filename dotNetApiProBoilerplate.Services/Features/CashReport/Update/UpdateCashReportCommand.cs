using Inventory.Dto.CashReports.Results;
using Inventory.Dto.CashReports.Requests;
using MediatR;

namespace Inventory.Services.Features.CashReport.Update
{
    public class UpdateCashReportCommand : IRequest<CashReportResult>
    {
        public Guid Id { get; }
        public UpdateCashReportRequest Request { get; }

        public UpdateCashReportCommand(Guid id, UpdateCashReportRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
