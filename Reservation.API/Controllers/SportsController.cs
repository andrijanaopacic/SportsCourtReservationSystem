using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.Application.Features.Sport.Commands;
using Reservation.Application.Features.Sport.Queries;

namespace Reservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SportsController : ControllerBase
    {
        private readonly IMediator mediator;

        public SportsController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? name)
        {
            var result = await mediator.Send(new GetAllSportsQuery(name));
            if (!result.IsSuccess) return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await mediator.Send(new GetSportByIdQuery(id));
            if (!result.IsSuccess) return NotFound(result.Error);

            return Ok(result.Value);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateSportCommand request)
        {
            var result = await mediator.Send(request);
            if (!result.IsSuccess) return BadRequest(result.Error);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.SportId }, result.Value);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSportRequestBody request)
        {
            var command = new UpdateSportCommand(id, request.Name, request.MaxPlayers);
            var result = await mediator.Send(command);
            if (!result.IsSuccess) return NotFound(result.Error);

            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await mediator.Send(new DeleteSportCommand(id));
            if (!result.IsSuccess) return BadRequest(result.Error);

            return NoContent();
        }
    }

    public record UpdateSportRequestBody(string Name, int MaxPlayers);
}