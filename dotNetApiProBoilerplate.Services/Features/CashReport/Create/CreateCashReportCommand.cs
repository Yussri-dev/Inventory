using Inventory.Dto.CashReports.Results;
using Inventory.Dto.CashReports.Requests;
using Inventory.Services.Features.CashReport.Create;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.CashReport.Create
{
    public class CreateCashReportCommand : IRequest<CashReportResult>
    {
        public CreateCashReportRequest Request { get; }

        public CreateCashReportCommand(CreateCashReportRequest request)
        {
            Request = request;
        }
    }
}
