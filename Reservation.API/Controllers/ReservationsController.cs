using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.API.DTOs.Reservation;
using Reservation.API.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using System.Security.Claims;

namespace Reservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ICacheService _cache;
        private readonly IValidator<CreateReservationRequest> _validator;

        private const string CacheKey = "reservations_all";
        private const string UserCacheKeyPrefix = "reservations_user_";
        private const string SingleCacheKeyPrefix = "reservation_";

        public ReservationsController(IUnitOfWork uow, ICacheService cache, IValidator<CreateReservationRequest> validator)
        {
            _uow = uow;
            _cache = cache;
            _validator = validator;
        }

        private static ReservationDto MapToDto(ReservationEntity r) => new ReservationDto
        {
            ReservationId = r.ReservationId,
            Status = r.Status.ToString(),
            TotalPrice = r.TotalPrice,
            ApplicationUserId = r.ApplicationUserId,
            Date = r.Date,
            Items = r.ReservationItems.Select(i => new ReservationItemDto
            {
                RowNumber = i.RowNumber,
                Price = i.TimeSlot.TotalPrice,
                Date = i.TimeSlot.Date,
                TimeSlotId = i.TimeSlotId,
                StartTime = i.TimeSlot?.StartTime.ToString(@"HH\:mm") ?? string.Empty,
                EndTime = i.TimeSlot?.EndTime.ToString(@"HH\:mm") ?? string.Empty,
                CourtName = i.TimeSlot?.Court?.Name ?? string.Empty
            }).ToList()
        };

        private void SyncStatus(ReservationEntity r)
        {
            if (r.Status == ReservationStatus.CANCELLED) return;
            if (!r.ReservationItems.Any()) return;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var dates = r.ReservationItems.Select(i => i.TimeSlot.Date).ToList();

            ReservationStatus updated;
            if (dates.All(d => d < today))
                updated = ReservationStatus.COMPLETED;
            else if (dates.Any(d => d == today))
                updated = ReservationStatus.ACTIVE;
            else
                updated = ReservationStatus.UPCOMING;

            if (r.Status != updated)
            {
                r.Status = updated;
                _uow.Reservations.Update(r);
                _uow.SaveChanges();
            }
        }

        private void SyncAll(IEnumerable<ReservationEntity> list)
        {
            foreach (var r in list) SyncStatus(r);
        }

        // Brise sve keševe vezane za jednu rezervaciju i njenog korisnika
        private async Task InvalidateCacheAsync(string userId, int? reservationId = null)
        {
            await _cache.RemoveAsync(CacheKey);
            await _cache.RemoveAsync($"{UserCacheKeyPrefix}{userId}");
            if (reservationId.HasValue)
                await _cache.RemoveAsync($"{SingleCacheKeyPrefix}{reservationId.Value}");
        }

        // GET /api/reservations?status=UPCOMING
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            // Keš koristimo samo kad nema filtera — filtrirani rezultati su podskup i mogu biti "zastareli"
            if (status == null)
            {
                var cached = await _cache.GetAsync<List<ReservationDto>>(CacheKey);
                if (cached != null)
                    return Ok(cached);
            }

            var all = _uow.Reservations.GetAll().ToList();
            SyncAll(all);

            var filtered = status != null && Enum.TryParse<ReservationStatus>(status, out var s)
                ? all.Where(r => r.Status == s)
                : all;

            var result = filtered.Select(MapToDto).ToList();

            // Keširamo samo nefiltriranu listu
            if (status == null)
                await _cache.SetAsync(CacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        // GET /api/reservations/my
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cacheKey = $"{UserCacheKeyPrefix}{userId}";

            var cached = await _cache.GetAsync<List<ReservationDto>>(cacheKey);
            if (cached != null)
                return Ok(cached);

            var reservations = _uow.Reservations.GetByUser(userId).ToList();
            SyncAll(reservations);

            var result = reservations.Select(MapToDto).ToList();
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        // GET /api/reservations/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cacheKey = $"{SingleCacheKeyPrefix}{id}";

            var cached = await _cache.GetAsync<ReservationDto>(cacheKey);
            if (cached != null)
                return Ok(cached);

            var reservation = _uow.Reservations.GetByIdWithItems(id);
            if (reservation == null) return NotFound();

            SyncStatus(reservation);

            var result = MapToDto(reservation);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(result);
        }

        // POST /api/reservations
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors
                    .Select(e => new { e.PropertyName, e.ErrorMessage }));

            var today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var item in request.Items)
            {
                var conflict = _uow.Reservations.GetAll()
                    .Where(r => r.Status != ReservationStatus.CANCELLED)
                    .SelectMany(r => r.ReservationItems)
                    .Any(i => i.TimeSlotId == item.TimeSlotId);

                if (conflict)
                    return BadRequest($"Termin {item} je već rezervisan.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = new List<ReservationItem>();
            int rowNumber = 1;

            foreach (var item in request.Items)
            {
                var slot = _uow.TimeSlots.GetById(item.TimeSlotId);

                if (slot == null)
                    return BadRequest($"TimeSlot {item.TimeSlotId} ne postoji.");

                if (slot.Date < today)
                    return BadRequest($"Termin za datum {slot.Date} je već prošao.");

                if (!slot.IsAvailable)
                    return BadRequest($"Termin {slot.TimeSlotId} nije dostupan.");

                slot.IsAvailable = false;

                items.Add(new ReservationItem
                {
                    RowNumber = rowNumber++,
                    TimeSlotId = slot.TimeSlotId,
                    Price = slot.TotalPrice,
                    TimeSlot = slot
                });
            }

            var dates = items.Select(i => i.TimeSlot.Date).ToList();
            ReservationStatus initialStatus;
            if (dates.All(d => d < today))
                initialStatus = ReservationStatus.COMPLETED;
            else if (dates.Any(d => d == today))
                initialStatus = ReservationStatus.ACTIVE;
            else
                initialStatus = ReservationStatus.UPCOMING;

            var reservation = new ReservationEntity
            {
                ApplicationUserId = userId,
                Status = initialStatus,
                ReservationItems = items,
                TotalPrice = items.Sum(i => i.Price),
                Date = today
            };

            _uow.Reservations.Add(reservation);
            _uow.SaveChanges();

            await InvalidateCacheAsync(userId);

            return Ok(MapToDto(reservation));
        }

        // PUT /api/reservations/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReservationRequest request)
        {
            var reservation = _uow.Reservations.GetByIdWithItems(id);
            if (reservation == null) return NotFound();

            if (reservation.Status == ReservationStatus.CANCELLED)
                return BadRequest("Nije moguće izmeniti otkazanu rezervaciju.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            reservation.Date = today;

            // ── Diff stavki ──────────────────────────────────────────────
            var existingItems = reservation.ReservationItems.ToList();

            var requestedKeys = request.Items
                .Select(i => i.TimeSlotId)
                .ToHashSet();

            var existingKeys = existingItems
                .Select(i => i.TimeSlotId)
                .ToHashSet();

            var toRemove = existingItems
                .Where(i => !requestedKeys.Contains(i.TimeSlotId))
                .ToList();

            var toAdd = request.Items
                .Where(i => !existingKeys.Contains(i.TimeSlotId))
                .ToList();

            // Konflikt check samo za nove stavke
            foreach (var item in toAdd)
            {
                var conflict = _uow.Reservations.GetAll()
                    .Where(r => r.ReservationId != id && r.Status != ReservationStatus.CANCELLED)
                    .SelectMany(r => r.ReservationItems)
                    .Any(i => i.TimeSlotId == item.TimeSlotId);

                if (conflict)
                    return BadRequest($"Termin {item.TimeSlotId} je već rezervisan.");
            }

            foreach (var item in toRemove)
            {
                var slot = _uow.TimeSlots.GetById(item.TimeSlotId);
                if (slot != null)
                    slot.IsAvailable = true;

                reservation.ReservationItems.Remove(item);
            }

            int nextRow = reservation.ReservationItems.Any()
                ? reservation.ReservationItems.Max(i => i.RowNumber) + 1
                : 1;

            foreach (var item in toAdd)
            {
                var slot = _uow.TimeSlots.GetById(item.TimeSlotId);

                if (slot == null)
                    return BadRequest($"TimeSlot {item.TimeSlotId} ne postoji.");

                if (slot.Date < today)
                    return BadRequest($"Termin za datum {slot.Date} je već prošao.");

                if (!slot.IsAvailable)
                    return BadRequest($"Termin {slot.TimeSlotId} nije dostupan.");

                slot.IsAvailable = false;
                reservation.ReservationItems.Add(new ReservationItem
                {
                    RowNumber = nextRow++,
                    TimeSlotId = item.TimeSlotId,
                    Price = slot.TotalPrice,
                    TimeSlot = slot
                });
            }

            if (!reservation.ReservationItems.Any())
            {
                _uow.Reservations.Remove(reservation);
                _uow.SaveChanges();
                await InvalidateCacheAsync(reservation.ApplicationUserId, id);
                return Ok();
            }

            reservation.TotalPrice = reservation.ReservationItems.Sum(i => i.Price);
            reservation.Status = ReservationStatus.UPCOMING;

            _uow.Reservations.Update(reservation);
            _uow.SaveChanges();

            SyncStatus(reservation);

            await InvalidateCacheAsync(reservation.ApplicationUserId, id);

            return Ok(MapToDto(reservation));
        }

        // PUT /api/reservations/{id}/cancel
        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = _uow.Reservations.GetByIdWithItems(id);
            if (reservation == null) return NotFound();

            if (reservation.Status == ReservationStatus.CANCELLED)
                return BadRequest("Rezervacija je već otkazana.");

            foreach (var item in reservation.ReservationItems)
            {
                var slot = _uow.TimeSlots.GetById(item.TimeSlotId);
                if (slot != null)
                    slot.IsAvailable = true;
            }

            reservation.Status = ReservationStatus.CANCELLED;
            _uow.Reservations.Update(reservation);
            _uow.SaveChanges();

            await InvalidateCacheAsync(reservation.ApplicationUserId, id);

            return Ok(MapToDto(reservation));
        }

        // GET /api/reservations/court/{courtId}?date=...
        [HttpGet("court/{courtId}")]
        public async Task<IActionResult> GetByCourtAndDate(int courtId, [FromQuery] DateOnly date)
        {
            var reservations = _uow.Reservations.GetAll()
                .Where(r => r.Status != ReservationStatus.CANCELLED)
                .Where(r => r.ReservationItems.Any(i => i.TimeSlot.CourtId == courtId && i.TimeSlot.Date == date))
                .ToList();

            return Ok(reservations.Select(MapToDto).ToList());
        }

        // GET /api/reservations/court/{courtId}/calendar?year=&month=
        [HttpGet("court/{courtId}/calendar")]
        public IActionResult GetCourtCalendar(int courtId, [FromQuery] int year, [FromQuery] int month)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var reservations = _uow.Reservations.GetAll()
                .Where(r => r.Status != ReservationStatus.CANCELLED)
                .Where(r => r.ReservationItems.Any(i =>
                    i.TimeSlot.CourtId == courtId &&
                    i.TimeSlot.Date >= startDate &&
                    i.TimeSlot.Date <= endDate))
                .ToList();

            var grouped = reservations
                .SelectMany(r => r.ReservationItems)
                .Where(i =>
                    i.TimeSlot.CourtId == courtId &&
                    i.TimeSlot.Date >= startDate &&
                    i.TimeSlot.Date <= endDate)
                .GroupBy(i => i.TimeSlot.Date)
                .Select(g => new CourtCalendarDayDTO
                {
                    Date = g.Key,
                    ReservationCount = g.Count()
                })
                .ToList();

            return Ok(grouped);
        }
    }
}
