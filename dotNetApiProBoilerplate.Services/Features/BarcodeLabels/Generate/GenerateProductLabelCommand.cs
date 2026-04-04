using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.BarcodeLabels.Generate
{
    public class GenerateProductLabelCommand : IRequest<byte[]>
    {
        public Guid ProductId { get; }

        public GenerateProductLabelCommand(Guid productId)
        {
            ProductId = productId;
        }
    }
}
