using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.BarcodeLabels.Generate
{
    public class GenerateProductLabelCommandHandler
    : IRequestHandler<GenerateProductLabelCommand, byte[]>
    {
        private readonly BarcodeLabelService _labelService;

        public GenerateProductLabelCommandHandler(BarcodeLabelService labelService)
        {
            _labelService = labelService;
        }

        public Task<byte[]> Handle(
            GenerateProductLabelCommand request,
            CancellationToken cancellationToken)
        {
            return _labelService.GenerateProductLabelAsync(request.ProductId);
        }
    }
}
