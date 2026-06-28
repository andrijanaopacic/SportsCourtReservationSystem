using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.Application.Features.Auth.Commands;
using Reservation.Application.Features.Auth.Queries;
using System.Security.Claims;

namespace Reservation.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator mediator;

        public AuthController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterCommand request)
        {
            var result = await mediator.Send(request);
            if (!result.IsSuccess) return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginCommand request)
        {
            var result = await mediator.Send(request);
            if (!result.IsSuccess) return Unauthorized(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await mediator.Send(new GetMeQuery(userId));
            if (!result.IsSuccess) return NotFound(result.Error);

            return Ok(result.Value);
        }
    }
}