using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.InventorySessions.Close
{
    public class CloseInventorySessionCommand : IRequest<bool>
    {
        public Guid Id { get; }

        public CloseInventorySessionCommand(Guid id)
        {
            Id = id;
        }
    }
}
