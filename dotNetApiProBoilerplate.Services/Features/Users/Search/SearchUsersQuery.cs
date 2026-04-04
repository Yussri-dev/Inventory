using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Users;
using MediatR;


namespace Inventory.Services.Features.Users.Search
{
    public class SearchUsersQuery : IRequest<PagedResult<UserResult>>
    {
        public UserQuery Query { get; }

        public SearchUsersQuery(UserQuery query)
        {
            Query = query;
        }
    }

}
