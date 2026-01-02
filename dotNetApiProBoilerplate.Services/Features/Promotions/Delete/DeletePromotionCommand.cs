using Inventory.Services.Features.Promotions.Delete;
using MediatR;

namespace Inventory.Services.Features.Promotions.Delete
{
    public class DeletePromotionCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeletePromotionCommand(Guid id)
        {
            Id = id;
        }
    }
}
