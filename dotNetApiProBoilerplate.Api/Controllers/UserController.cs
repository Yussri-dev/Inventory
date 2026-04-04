using AutoMapper.Features;
using Inventory.Dto.Queries;
using Inventory.Dto.Users;
using Inventory.Dto.Users.Requests;
using Inventory.Services.Features.Users.Activate;
using Inventory.Services.Features.Users.Deactivate;
using Inventory.Services.Features.Users.Delete;
using Inventory.Services.Features.Users.GetAllUsers;
using Inventory.Services.Features.Users.GetById;
using Inventory.Services.Features.Users.Search;
using Inventory.Services.Features.Users.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/users")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/v1/users
        [HttpGet]
        [ProducesResponseType(typeof(List<UserResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllUsersQuery(), cancellationToken);
            return Ok(result);
        }

        // GET api/v1/users/{id}
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        // GET api/v1/users/search
        [HttpGet("search")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] UserQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new SearchUsersQuery(query), cancellationToken);
            return Ok(result);
        }

        // PUT api/v1/users/{id}
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(UserResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(new UpdateUserCommand(id, request), cancellationToken);
            return Ok(result);
        }

        // PUT api/v1/users/{id}/deactivate
        [HttpPut("{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeactivateUserCommand(id), cancellationToken);
            return Ok(new { message = "User deactivated." });
        }

        // PUT api/v1/users/{id}/activate
        [HttpPut("{id:guid}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new ActivateUserCommand(id), cancellationToken);
            return Ok(new { message = "User activated." });
        }

        // DELETE api/v1/users/{id}
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteUserCommand(id), cancellationToken);
            return NoContent();
        }
    }
}