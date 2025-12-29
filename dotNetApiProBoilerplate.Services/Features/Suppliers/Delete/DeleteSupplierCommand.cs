using Inventory.Services.Features.Suppliers.Delete;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Suppliers.Delete
{
    public class DeleteSupplierCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteSupplierCommand(Guid id)
        {
            Id = id;
        }
    }
}
