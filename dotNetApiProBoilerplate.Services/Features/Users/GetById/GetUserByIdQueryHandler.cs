using Inventory.Dto.Users;
using MediatR;


namespace Inventory.Services.Features.Users.GetById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserResult>
    {
        private readonly UserService _userService;
        public GetUserByIdQueryHandler(UserService userService) => _userService = userService;
        public async Task<UserResult> Handle(GetUserByIdQuery query, CancellationToken ct)
        {
            return await _userService.GetByIdAsync(query.Id);
        }
    }
}
