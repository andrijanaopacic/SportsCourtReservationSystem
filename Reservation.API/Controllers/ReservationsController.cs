using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationCommands = Reservation.Application.Features.Reservation.Commands;
using ReservationQueries = Reservation.Application.Features.Reservation.Queries;
using System.Security.Claims;

namespace Reservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IMediator mediator;

        public ReservationsController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var result = await mediator.Send(new ReservationQueries.GetAllReservationsQuery(status));
            if (!result.IsSuccess) return BadRequest(result.Error);
            return Ok(result.Value);
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await mediator.Send(new ReservationQueries.GetMyReservationsQuery(userId));
            if (!result.IsSuccess) return BadRequest(result.Error);
            return Ok(result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await mediator.Send(new ReservationQueries.GetReservationByIdQuery(id));
            if (!result.IsSuccess) return NotFound();
            return Ok(result.Value);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateReservationRequestBody request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var command = new ReservationCommands.CreateReservationCommand(userId, request.Items);

            var result = await mediator.Send(command);
            if (!result.IsSuccess) return BadRequest(result.Error);
            return Ok(result.Value);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReservationRequestBody request)
        {
            var command = new ReservationCommands.UpdateReservationCommand(id, request.Items);

            var result = await mediator.Send(command);
            if (!result.IsSuccess) return NotFound(result.Error);
            return Ok(result.Value);
        }

        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await mediator.Send(new ReservationCommands.CancelReservationCommand(id));
            if (!result.IsSuccess) return NotFound(result.Error);
            return Ok(result.Value);
        }

        [HttpGet("court/{courtId}")]
        public async Task<IActionResult> GetByCourtAndDate(int courtId, [FromQuery] DateOnly date)
        {
            var result = await mediator.Send(new ReservationQueries.GetReservationsByCourtAndDateQuery(courtId, date));
            if (!result.IsSuccess) return BadRequest(result.Error);
            return Ok(result.Value);
        }

        [HttpGet("court/{courtId}/calendar")]
        public async Task<IActionResult> GetCourtCalendar(int courtId, [FromQuery] int year, [FromQuery] int month)
        {
            var result = await mediator.Send(new ReservationQueries.GetCourtCalendarQuery(courtId, year, month));
            if (!result.IsSuccess) return BadRequest(result.Error);
            return Ok(result.Value);
        }

        [HttpGet("court/{courtId}/slots")]
        public async Task<IActionResult> GetSlotsByCourtAndDate(int courtId, [FromQuery] DateOnly date)
        {
            var result = await mediator.Send(new ReservationQueries.GetSlotsByCourtAndDateQuery(courtId, date));
            if (!result.IsSuccess) return BadRequest(result.Error);
            return Ok(result.Value);
        }
    }

    public record CreateReservationRequestBody(List<ReservationCommands.ReservationItemInput> Items);
    public record UpdateReservationRequestBody(List<ReservationCommands.ReservationItemInput> Items);
}