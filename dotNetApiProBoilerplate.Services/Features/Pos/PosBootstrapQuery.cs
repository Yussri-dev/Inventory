using Inventory.Dto.GlobalRequests.Results;
using MediatR;

namespace Inventory.Services.Features.Pos
{
    public sealed record PosBootstrapQuery : IRequest<PosBootstrapResult>;
}
