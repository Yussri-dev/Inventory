using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.InventorySessions.Validate
{
    public class ValidateInventorySessionCommand : IRequest<bool>
    {
        public Guid Id { get; }

        public ValidateInventorySessionCommand(Guid id)
        {
            Id = id;
        }
    }
}
