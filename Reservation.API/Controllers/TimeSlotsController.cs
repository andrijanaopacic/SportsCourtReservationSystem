using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.API.DTOs.TimeSlot;
using Reservation.API.Extensions;
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

        private const string AllSlotsKeyPrefix = "timeslots:all:";
        private const string SlotByCourtPrefix = "timeslots:court:";
        private const string SlotByIdPrefix = "timeslots:id:";
        private const string AvailableSlotsKey = "timeslots:available";

        public TimeSlotsController(
            IUnitOfWork uow,
            IValidator<CreateTimeSlotRequest> validator,
            ICacheService cache)
        {
            _uow = uow;
            _validator = validator;
            _cache = cache;
        }

        private static decimal ComputeTotalPrice(decimal price, TimeOnly start, TimeOnly end)
        {
            var hours = (decimal)(end - start).TotalHours;
            return hours > 0 ? price * hours : 0;
        }

        private static TimeSlotDto MapToDto(TimeSlot t, string courtName) => new()
        {
            TimeSlotId = t.TimeSlotId,
            Date = t.Date,
            StartTime = t.StartTime,
            EndTime = t.EndTime,
            Duration = t.Duration,
            Price = t.Price,
            TotalPrice = t.TotalPrice,
            IsAvailable = t.IsAvailable,
            CourtId = t.CourtId,
            CourtName = courtName
        };

        // GET /api/timeslots?isAvailable=true&minPrice=500&maxPrice=2000
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool? isAvailable,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice)
        {
            var cacheKey = $"{AllSlotsKeyPrefix}{isAvailable}:{minPrice}:{maxPrice}";
            var cached = await _cache.GetAsync<List<TimeSlotDto>>(cacheKey);
            if (cached != null) return Ok(cached);

            var slots = _uow.TimeSlots.GetAll()
                .FilterByAvailability(isAvailable)
                .FilterByMinPrice(minPrice)
                .FilterByMaxPrice(maxPrice)
                .Select(t => MapToDto(t, t.Court?.Name ?? string.Empty))
                .ToList();

            await _cache.SetAsync(cacheKey, slots, TimeSpan.FromMinutes(10));
            return Ok(slots);
        }

        // GET /api/timeslots/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var cacheKey = SlotByIdPrefix + id;
            var cached = await _cache.GetAsync<TimeSlotDto>(cacheKey);
            if (cached != null) return Ok(cached);

            var slot = _uow.TimeSlots.GetByIdWithCourt(id);
            if (slot == null) return NotFound($"Time slot with ID {id} was not found.");

            var dto = MapToDto(slot, slot.Court?.Name ?? string.Empty);
            await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
            return Ok(dto);
        }

        // GET /api/timeslots/by-court/{courtId}
        [HttpGet("by-court/{courtId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCourt(int courtId)
        {
            var court = _uow.Courts.GetById(courtId);
            if (court == null) return NotFound($"Court with ID {courtId} was not found.");

            var cacheKey = SlotByCourtPrefix + courtId;
            var cached = await _cache.GetAsync<List<TimeSlotDto>>(cacheKey);
            if (cached != null) return Ok(cached);

            var slots = _uow.TimeSlots.GetByCourt(courtId)
                .Select(t => MapToDto(t, court.Name))
                .ToList();

            await _cache.SetAsync(cacheKey, slots, TimeSpan.FromMinutes(10));
            return Ok(slots);
        }

        // GET /api/timeslots/available
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailable()
        {
            var cached = await _cache.GetAsync<List<TimeSlotDto>>(AvailableSlotsKey);
            if (cached != null) return Ok(cached);

            var slots = _uow.TimeSlots.GetAll()
                .Where(t => t.IsAvailable)
                .Select(t => MapToDto(t, t.Court?.Name ?? string.Empty))
                .ToList();

            await _cache.SetAsync(AvailableSlotsKey, slots, TimeSpan.FromMinutes(10));
            return Ok(slots);
        }

        // POST /api/timeslots — Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTimeSlotRequest request)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors
                    .Select(e => new { e.PropertyName, e.ErrorMessage }));

            var court = _uow.Courts.GetById(request.CourtId);
            if (court == null) return NotFound($"Court with ID {request.CourtId} was not found.");

            var overlapping = _uow.TimeSlots.GetByCourt(request.CourtId)
                .Any(t => t.Date == request.Date &&
                          t.StartTime < request.EndTime &&
                          t.EndTime > request.StartTime);

            if (overlapping)
                return BadRequest("An overlapping time slot already exists on this court for that date.");

            var slot = new TimeSlot
            {
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Duration = request.EndTime - request.StartTime,
                Price = request.Price,
                TotalPrice = ComputeTotalPrice(request.Price, request.StartTime, request.EndTime),
                IsAvailable = request.IsAvailable,
                CourtId = request.CourtId
            };

            _uow.TimeSlots.Add(slot);
            _uow.SaveChanges();

            await _cache.RemoveByPatternAsync(AllSlotsKeyPrefix + "*");
            await _cache.RemoveAsync(SlotByCourtPrefix + request.CourtId);
            await _cache.RemoveAsync(AvailableSlotsKey);

            return CreatedAtAction(nameof(GetById), new { id = slot.TimeSlotId }, MapToDto(slot, court.Name));
        }

        // PUT /api/timeslots/{id} — Admin only
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateTimeSlotRequest request)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors
                    .Select(e => new { e.PropertyName, e.ErrorMessage }));

            var slot = _uow.TimeSlots.GetById(id);
            if (slot == null) return NotFound($"Time slot with ID {id} was not found.");

            var court = _uow.Courts.GetById(request.CourtId);
            if (court == null) return NotFound($"Court with ID {request.CourtId} was not found.");

            var overlapping = _uow.TimeSlots.GetByCourt(request.CourtId)
                .Any(t => t.TimeSlotId != id &&
                          t.Date == request.Date &&
                          t.StartTime < request.EndTime &&
                          t.EndTime > request.StartTime);

            if (overlapping)
                return BadRequest("An overlapping time slot already exists on this court for that date.");

            var oldCourtId = slot.CourtId;

            slot.Date = request.Date;
            slot.StartTime = request.StartTime;
            slot.EndTime = request.EndTime;
            slot.Duration = request.EndTime - request.StartTime;
            slot.Price = request.Price;
            slot.TotalPrice = ComputeTotalPrice(request.Price, request.StartTime, request.EndTime);
            slot.IsAvailable = request.IsAvailable;
            slot.CourtId = request.CourtId;

            _uow.TimeSlots.Update(slot);
            _uow.SaveChanges();

            await _cache.RemoveByPatternAsync(AllSlotsKeyPrefix + "*");
            await _cache.RemoveAsync(SlotByIdPrefix + id);
            await _cache.RemoveAsync(SlotByCourtPrefix + request.CourtId);
            await _cache.RemoveAsync(AvailableSlotsKey);
            if (oldCourtId != request.CourtId)
                await _cache.RemoveAsync(SlotByCourtPrefix + oldCourtId);

            return Ok(MapToDto(slot, court.Name));
        }

        // DELETE /api/timeslots/{id} — Admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var slot = _uow.TimeSlots.GetById(id);
            if (slot == null) return NotFound($"Time slot with ID {id} was not found.");

            var courtId = slot.CourtId;

            _uow.TimeSlots.Remove(slot);
            _uow.SaveChanges();

            await _cache.RemoveByPatternAsync(AllSlotsKeyPrefix + "*");
            await _cache.RemoveAsync(SlotByIdPrefix + id);
            await _cache.RemoveAsync(SlotByCourtPrefix + courtId);
            await _cache.RemoveAsync(AvailableSlotsKey);

            return NoContent();
        }
    }
}
