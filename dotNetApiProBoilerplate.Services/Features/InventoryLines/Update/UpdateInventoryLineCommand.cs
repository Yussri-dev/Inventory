using Inventory.Dto.InventoryLines.Results;
using Inventory.Dto.InventoryLines.Requests;
using Inventory.Services.Features.InventoryLines.Update;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.InventoryLines.Update
{
    public class UpdateInventoryLineCommand : IRequest<InventoryLineResult>
    {
        public Guid Id { get; }
        public UpdateInventoryLineRequest Request { get; }

        public UpdateInventoryLineCommand(Guid id, UpdateInventoryLineRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
