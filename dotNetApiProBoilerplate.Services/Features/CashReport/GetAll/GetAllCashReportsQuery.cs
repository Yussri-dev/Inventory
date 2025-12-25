using Inventory.Dto.CashReports.Results;
using Inventory.Services.Features.CashReport.GetAll;
using MediatR;

namespace Inventory.Services.Features.CashReport.GetAll
{
    public class GetAllCashReportsQuery : IRequest<List<CashReportResult>>
    {
    }
}
