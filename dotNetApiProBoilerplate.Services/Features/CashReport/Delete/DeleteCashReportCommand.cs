using Inventory.Services.Features.CashReport.Delete;
using MediatR;

namespace Inventory.Services.Features.CashReport.Delete
{
    public class DeleteCashReportCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteCashReportCommand(Guid id)
        {
            Id = id;
        }
    }
}
