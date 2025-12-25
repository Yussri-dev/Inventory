using Inventory.Services.Features.CashSession.Delete;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.CashSession.Delete
{
    public class DeleteCashSessionCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteCashSessionCommand(Guid id)
        {
            Id = id;
        }
    }
}
