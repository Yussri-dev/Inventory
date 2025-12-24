using Inventory.Services.Features.CashMovement.Delete;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.CashMovement.Delete
{
    public class DeleteCashMovementCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteCashMovementCommand(Guid id)
        {
            Id = id;
        }
    }
}
