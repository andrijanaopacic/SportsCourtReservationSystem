using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.Application.Features.Court.Commands;
using Reservation.Application.Features.Court.Queries;

namespace Reservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CourtsController : ControllerBase
    {
        private readonly IMediator mediator;

        public CourtsController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? name, [FromQuery] bool? isIndoor, [FromQuery] int? sportId)
        {
            var result = await mediator.Send(new GetAllCourtsQuery(name, isIndoor, sportId));
            if (!result.IsSuccess) return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await mediator.Send(new GetCourtByIdQuery(id));
            if (!result.IsSuccess) return NotFound(result.Error);

            return Ok(result.Value);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCourtCommand request)
        {
            var result = await mediator.Send(request);
            if (!result.IsSuccess) return BadRequest(result.Error);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.CourtId }, result.Value);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCourtRequestBody request)
        {
            var command = new UpdateCourtCommand(
                id, request.Name, request.Location, request.Description,
                request.PricePerHour, request.IsIndoor, request.SportId);

            var result = await mediator.Send(command);
            if (!result.IsSuccess) return NotFound(result.Error);

            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await mediator.Send(new DeleteCourtCommand(id));
            if (!result.IsSuccess) return BadRequest(result.Error);

            return NoContent();
        }
    }

    public record UpdateCourtRequestBody(
        string Name, string Location, string Description,
        decimal PricePerHour, bool IsIndoor, int SportId);
}