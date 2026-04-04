

using Inventory.Dto.Suppliers.Results;
using Inventory.Dto.Users;
using Inventory.Services.Features.Suppliers.GetAll;
using MediatR;

namespace Inventory.Services.Features.Users.GetAllUsers
{
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<UserResult>>
    {
        private readonly UserService _userService;

        public GetAllUsersQueryHandler(UserService userService)
        {
            _userService = userService;
        }

        public Task<List<UserResult>> Handle(GetAllUsersQuery query, CancellationToken cancellationToken)
        {
            return _userService.GetAllAsync();
        }
    }
}
