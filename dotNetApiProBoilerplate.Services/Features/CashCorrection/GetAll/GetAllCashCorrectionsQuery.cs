using Inventory.Dto.CashCorrections.Results;
using Inventory.Dto.Customers.Results;
using Inventory.Services.Features.Customers.GetAll;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.CashCorrection.GetAll
{
    public class GetAllCashCorrectionsQuery : IRequest<List<CashCorrectionResult>>
    {
    }
}
