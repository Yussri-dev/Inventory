using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.ProductCategory.Delete
{
    public class DeleteProductCategoryCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteProductCategoryCommand(Guid id)
        {
            Id = id;
        }
    }

}
