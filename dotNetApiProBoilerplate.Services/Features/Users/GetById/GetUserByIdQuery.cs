using Inventory.Dto.Users;
using MediatR;


namespace Inventory.Services.Features.Users.GetById
{
    public class GetUserByIdQuery : IRequest<UserResult>
    {
        public Guid Id { get; }

        public GetUserByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
