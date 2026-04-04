using Inventory.Dto.Users;
using MediatR;


namespace Inventory.Services.Features.Users.GetAllUsers
{
    public class GetAllUsersQuery : IRequest<List<UserResult>>
    {
    }
    
}
