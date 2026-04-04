using Inventory.Dto.Users;
using Inventory.Dto.Users.Requests;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Users.Update
{
    public class UpdateUserCommand : IRequest<UserResult>
    {
        public Guid Id { get; }

        public UpdateUserRequest Request { get; }

        public UpdateUserCommand(Guid id, UpdateUserRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
