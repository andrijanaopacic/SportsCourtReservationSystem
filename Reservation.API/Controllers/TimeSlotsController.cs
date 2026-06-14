using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.API.DTOs.TimeSlot;
using Reservation.API.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeSlotsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IValidator<CreateTimeSlotRequest> _validator;
        private readonly ICacheService _cache;

        private const string AllSlotsKey = "timeslots:all";
        private const string SlotByCourtPrefix = "timeslots:court:";
        private const string SlotByIdPrefix = "timeslots:id:";

        public TimeSlotsController(
            IUnitOfWork uow,
            IValidator<CreateTimeSlotRequest> validator,
            ICacheService cache)
        {
            _uow = uow;
            _validator = validator;
            _cache = cache;
        }

        // GET /api/timeslots
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var cached = await _cache.GetAsync<List<TimeSlotDto>>(AllSlotsKey);
            if (cached != null)
                return Ok(cached);

            var slots = _uow.TimeSlots.GetAll()
                .Select(t => new TimeSlotDto
                {
                    TimeSlotId = t.TimeSlotId,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    CourtId = t.CourtId,
                    CourtName = t.Court?.Name ?? string.Empty
                })
                .ToList();

            await _cache.SetAsync(AllSlotsKey, slots, TimeSpan.FromMinutes(10));
            return Ok(slots);
        }

        // GET /api/timeslots/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var cacheKey = SlotByIdPrefix + id;
            var cached = await _cache.GetAsync<TimeSlotDto>(cacheKey);
            if (cached != null)
                return Ok(cached);

            var slot = _uow.TimeSlots.GetByIdWithCourt(id);
            if (slot == null) return NotFound($"Termin sa id {id} nije pronađen.");

            var dto = new TimeSlotDto
            {
                TimeSlotId = slot.TimeSlotId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                CourtId = slot.CourtId,
                CourtName = slot.Court?.Name ?? string.Empty
            };

            await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
            return Ok(dto);
        }

        // GET /api/timeslots/by-court/{courtId}
        [HttpGet("by-court/{courtId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCourt(int courtId)
        {
            var court = _uow.Courts.GetById(courtId);
            if (court == null) return NotFound($"Teren sa id {courtId} nije pronađen.");

            var cacheKey = SlotByCourtPrefix + courtId;
            var cached = await _cache.GetAsync<List<TimeSlotDto>>(cacheKey);
            if (cached != null)
                return Ok(cached);

            var slots = _uow.TimeSlots.GetByCourt(courtId)
                .Select(t => new TimeSlotDto
                {
                    TimeSlotId = t.TimeSlotId,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    CourtId = t.CourtId,
                    CourtName = court.Name
                })
                .ToList();

            await _cache.SetAsync(cacheKey, slots, TimeSpan.FromMinutes(10));
            return Ok(slots);
        }

        // POST /api/timeslots —> only Admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTimeSlotRequest request)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors
                    .Select(e => new { e.PropertyName, e.ErrorMessage }));

            var court = _uow.Courts.GetById(request.CourtId);
            if (court == null) return NotFound($"Teren sa id {request.CourtId} nije pronađen.");

            var overlapping = _uow.TimeSlots.GetByCourt(request.CourtId)
                .Any(t => t.StartTime < request.EndTime && t.EndTime > request.StartTime);

            if (overlapping)
                return BadRequest("Na ovom terenu već postoji termin koji se preklapa sa zadatim vremenom.");

            var slot = new TimeSlot
            {
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                CourtId = request.CourtId
            };

            _uow.TimeSlots.Add(slot);
            _uow.SaveChanges();

            await _cache.RemoveAsync(AllSlotsKey);
            await _cache.RemoveAsync(SlotByCourtPrefix + request.CourtId);

            return CreatedAtAction(nameof(GetById), new { id = slot.TimeSlotId }, new TimeSlotDto
            {
                TimeSlotId = slot.TimeSlotId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                CourtId = slot.CourtId,
                CourtName = court.Name
            });
        }

        // PUT /api/timeslots/{id} —> only Admin
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateTimeSlotRequest request)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors
                    .Select(e => new { e.PropertyName, e.ErrorMessage }));

            var slot = _uow.TimeSlots.GetById(id);
            if (slot == null) return NotFound($"Termin sa id {id} nije pronađen.");

            var court = _uow.Courts.GetById(request.CourtId);
            if (court == null) return NotFound($"Teren sa id {request.CourtId} nije pronađen.");

            var overlapping = _uow.TimeSlots.GetByCourt(request.CourtId)
                .Any(t => t.TimeSlotId != id &&
                          t.StartTime < request.EndTime &&
                          t.EndTime > request.StartTime);

            if (overlapping)
                return BadRequest("Na ovom terenu već postoji termin koji se preklapa sa zadatim vremenom.");

            var oldCourtId = slot.CourtId;

            slot.StartTime = request.StartTime;
            slot.EndTime = request.EndTime;
            slot.CourtId = request.CourtId;

            _uow.TimeSlots.Update(slot);
            _uow.SaveChanges();

            await _cache.RemoveAsync(AllSlotsKey);
            await _cache.RemoveAsync(SlotByIdPrefix + id);
            await _cache.RemoveAsync(SlotByCourtPrefix + request.CourtId);
            if (oldCourtId != request.CourtId)
                await _cache.RemoveAsync(SlotByCourtPrefix + oldCourtId);

            return Ok(new TimeSlotDto
            {
                TimeSlotId = slot.TimeSlotId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                CourtId = slot.CourtId,
                CourtName = court.Name
            });
        }

        // DELETE /api/timeslots/{id} — only Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var slot = _uow.TimeSlots.GetById(id);
            if (slot == null) return NotFound($"Termin sa id {id} nije pronađen.");

            var courtId = slot.CourtId;

            _uow.TimeSlots.Remove(slot);
            _uow.SaveChanges();

            await _cache.RemoveAsync(AllSlotsKey);
            await _cache.RemoveAsync(SlotByIdPrefix + id);
            await _cache.RemoveAsync(SlotByCourtPrefix + courtId);

            return NoContent();
        }
    }
}
