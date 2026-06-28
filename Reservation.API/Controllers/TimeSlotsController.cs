using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Application.Features.TimeSlot.Queries;

namespace Reservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeSlotsController : ControllerBase
    {
        private readonly IMediator mediator;

        public TimeSlotsController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        // GET /api/timeslots?isAvailable=true&minPrice=500&maxPrice=2000
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool? isAvailable,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice)
        {
            var result = await mediator.Send(new GetAllTimeSlotsQuery(isAvailable, minPrice, maxPrice));
            if (!result.IsSuccess) return BadRequest(result.Error);

            return Ok(result.Value);
        }

        // GET /api/timeslots/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await mediator.Send(new GetTimeSlotByIdQuery(id));
            if (!result.IsSuccess) return NotFound(result.Error);

            return Ok(result.Value);
        }

        // GET /api/timeslots/by-court/{courtId}
        [HttpGet("by-court/{courtId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCourt(int courtId)
        {
            var result = await mediator.Send(new GetTimeSlotsByCourtQuery(courtId));
            if (!result.IsSuccess) return NotFound(result.Error);

            return Ok(result.Value);
        }

        // GET /api/timeslots/available
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailable()
        {
            var result = await mediator.Send(new GetAvailableTimeSlotsQuery());
            if (!result.IsSuccess) return BadRequest(result.Error);

            return Ok(result.Value);
        }

        // POST /api/timeslots — Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTimeSlotCommand request)
        {
            var result = await mediator.Send(request);
            if (!result.IsSuccess) return BadRequest(result.Error);

            return CreatedAtAction(nameof(GetById), new { id = result.Value!.TimeSlotId }, result.Value);
        }

        // PUT /api/timeslots/{id} — Admin only
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTimeSlotRequestBody request)
        {
            var command = new UpdateTimeSlotCommand(
                id, request.Date, request.StartTime, request.EndTime, request.CourtId, request.IsAvailable);

            var result = await mediator.Send(command);
            if (!result.IsSuccess) return NotFound(result.Error);

            return Ok(result.Value);
        }

        // DELETE /api/timeslots/{id} — Admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await mediator.Send(new DeleteTimeSlotCommand(id));
            if (!result.IsSuccess) return BadRequest(result.Error);

            return NoContent();
        }
    }

    public record UpdateTimeSlotRequestBody(
        DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, int CourtId, bool IsAvailable);
}