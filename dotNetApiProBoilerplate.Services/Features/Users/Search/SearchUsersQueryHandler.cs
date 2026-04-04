using Inventory.Dto.Pages.Results;
using Inventory.Dto.Users;
using MediatR;


namespace Inventory.Services.Features.Users.Search
{
    public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, PagedResult<UserResult>>
    {
        private readonly UserService _userService;
        public SearchUsersQueryHandler(UserService userService) => _userService = userService;
        public async Task<PagedResult<UserResult>> Handle(SearchUsersQuery query, CancellationToken ct)
        {
            return await _userService.QueryAsync(query.Query);
        }
    }
}
