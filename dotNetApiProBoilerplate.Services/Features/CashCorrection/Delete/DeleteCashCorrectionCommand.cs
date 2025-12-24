using Inventory.Services.Features.Customers.Delete;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.CashCorrection.Delete
{
    public class DeleteCashCorrectionCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteCashCorrectionCommand(Guid id)
        {
            Id = id;
        }
    }
}
