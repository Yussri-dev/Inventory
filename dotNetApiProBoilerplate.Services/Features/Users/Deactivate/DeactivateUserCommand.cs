using Inventory.Dto.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.Users.Deactivate
{
    public class DeactivateUserCommand : IRequest<UserResult>
    {
        public Guid Id { get; }
        public DeactivateUserCommand(Guid id)
        {
            Id = id;
        }
    }
}
