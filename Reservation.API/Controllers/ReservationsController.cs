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
        private const string CacheKey = "reservations_all";
        private const string UserCacheKeyPrefix = "reservations_user_";

        public ReservationsController(IUnitOfWork uow, ICacheService cache)
        {
            _uow = uow;
            _cache = cache;
        }

        // ── Automatsko ažuriranje statusa na osnovu datuma ────────────────
        private void SyncStatus(ReservationEntity r)
        {
            if (r.Status == ReservationStatus.CANCELLED) return;
            if (!r.ReservationItems.Any()) return;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var dates = r.ReservationItems.Select(i => i.Date).ToList();

            ReservationStatus computed;
            if (dates.All(d => d < today))
                computed = ReservationStatus.COMPLETED;
            else if (dates.Any(d => d == today))
                computed = ReservationStatus.ACTIVE;
            else
                computed = ReservationStatus.UPCOMING;

            if (r.Status != computed)
            {
                r.Status = computed;
                _uow.Reservations.Update(r);
                _uow.SaveChanges();
            }
        }

        private void SyncAll(IEnumerable<ReservationEntity> list)
        {
            foreach (var r in list) SyncStatus(r);
        }

        // GET /api/reservations?status=UPCOMING
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var all = _uow.Reservations.GetAll().ToList();
            SyncAll(all);

            var filtered = status != null && Enum.TryParse<ReservationStatus>(status, out var s)
                ? all.Where(r => r.Status == s)
                : all;

            return Ok(filtered.Select(MapToDto).ToList());
        }

        // GET /api/reservations/my
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var reservations = _uow.Reservations.GetByUser(userId).ToList();
            SyncAll(reservations);
            return Ok(reservations.Select(MapToDto).ToList());
        }

        // GET /api/reservations/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var reservation = _uow.Reservations.GetByIdWithItems(id);
            if (reservation == null) return NotFound();
            SyncStatus(reservation);
            return Ok(MapToDto(reservation));
        }

        // POST /api/reservations
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
        {
            // Duplikat unutar requesta
            var duplicateInRequest = request.Items
                .GroupBy(i => (i.TimeSlotId, i.Date))
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateInRequest != null)
                return BadRequest($"Termin {duplicateInRequest.Key.TimeSlotId} se pojavljuje više puta za datum {duplicateInRequest.Key.Date}.");
            
            var today = DateOnly.FromDateTime(DateTime.Today);
            var pastItems = request.Items.Where(i => i.Date < today).ToList();

            if (pastItems.Any())
                return BadRequest($"Ne možete rezervisati termine za prošle datume. Datum {pastItems.First().Date} je već prošao.");

            // Konflikt u bazi
            foreach (var item in request.Items)
            {
                var conflict = _uow.Reservations.GetAll()
                    .Where(r => r.Status != ReservationStatus.CANCELLED)
                    .SelectMany(r => r.ReservationItems)
                    .Any(i => i.TimeSlotId == item.TimeSlotId && i.Date == item.Date);

                if (conflict)
                    return BadRequest($"Termin {item.TimeSlotId} je već rezervisan za {item.Date}.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var items = request.Items.Select((item, idx) => new ReservationItem
            {
                RowNumber = idx + 1,
                TimeSlotId = item.TimeSlotId,
                Date = item.Date,
                Price = _uow.TimeSlots.GetById(item.TimeSlotId)?.Price ?? 0
            }).ToList();

            // Odmah postavi ispravan status
            var dates = items.Select(i => i.Date).ToList();
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
                TotalPrice = items.Sum(i => i.Price)
            };

            _uow.Reservations.Add(reservation);
            _uow.SaveChanges();

            await _cache.RemoveAsync(CacheKey);
            await _cache.RemoveAsync($"{UserCacheKeyPrefix}{userId}");

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

            if (!Enum.TryParse<ReservationStatus>(request.Status, out var newStatus))
                return BadRequest($"Nepoznat status: {request.Status}.");

            // Duplikat unutar requesta
            var duplicateInRequest = request.Items
                .GroupBy(i => (i.TimeSlotId, i.Date))
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateInRequest != null)
                return BadRequest($"Termin {duplicateInRequest.Key.TimeSlotId} se pojavljuje više puta za datum {duplicateInRequest.Key.Date}.");

            // ── Diff stavki ──────────────────────────────────────────────
            var existingItems = reservation.ReservationItems.ToList();

            var requestedKeys = request.Items
                .Select(i => (i.TimeSlotId, i.Date))
                .ToHashSet();

            var existingKeys = existingItems
                .Select(i => (i.TimeSlotId, i.Date))
                .ToHashSet();

            var toRemove = existingItems
                .Where(i => !requestedKeys.Contains((i.TimeSlotId, i.Date)))
                .ToList();

            var toAdd = request.Items
                .Where(i => !existingKeys.Contains((i.TimeSlotId, i.Date)))
                .ToList();

            // Konflikt check samo za nove stavke
            foreach (var item in toAdd)
            {
                var conflict = _uow.Reservations.GetAll()
                    .Where(r => r.ReservationId != id && r.Status != ReservationStatus.CANCELLED)
                    .SelectMany(r => r.ReservationItems)
                    .Any(i => i.TimeSlotId == item.TimeSlotId && i.Date == item.Date);

                if (conflict)
                    return BadRequest($"Termin {item.TimeSlotId} je već rezervisan za {item.Date}.");
            }

            foreach (var item in toRemove)
                reservation.ReservationItems.Remove(item);

            int nextRow = reservation.ReservationItems.Any()
                ? reservation.ReservationItems.Max(i => i.RowNumber) + 1
                : 1;

            foreach (var item in toAdd)
            {
                reservation.ReservationItems.Add(new ReservationItem
                {
                    RowNumber = nextRow++,
                    TimeSlotId = item.TimeSlotId,
                    Date = item.Date,
                    Price = _uow.TimeSlots.GetById(item.TimeSlotId)?.Price ?? 0
                });
            }

            reservation.TotalPrice = reservation.ReservationItems.Sum(i => i.Price);
            // Status koji admin/klijent šalje se prihvata, ali se ne overriduje automatskim —
            // SyncStatus će to srediti pri sledećem GET-u
            reservation.Status = newStatus;

            _uow.Reservations.Update(reservation);
            _uow.SaveChanges();

            // Odmah sinhronizuj nakon snimanja
            SyncStatus(reservation);

            await _cache.RemoveAsync(CacheKey);
            await _cache.RemoveAsync($"{UserCacheKeyPrefix}{reservation.ApplicationUserId}");

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

            reservation.Status = ReservationStatus.CANCELLED;
            _uow.Reservations.Update(reservation);
            _uow.SaveChanges();

            await _cache.RemoveAsync(CacheKey);
            await _cache.RemoveAsync($"{UserCacheKeyPrefix}{reservation.ApplicationUserId}");

            return Ok(MapToDto(reservation));
        }

        private static ReservationDto MapToDto(ReservationEntity r) => new ReservationDto
        {
            ReservationId = r.ReservationId,
            Status = r.Status.ToString(),
            TotalPrice = r.TotalPrice,
            ApplicationUserId = r.ApplicationUserId,
            Items = r.ReservationItems.Select(i => new ReservationItemDto
            {
                RowNumber = i.RowNumber,
                Price = i.Price,
                Date = i.Date,
                TimeSlotId = i.TimeSlotId,
                StartTime = i.TimeSlot?.StartTime.ToString(@"HH\:mm") ?? string.Empty,
                EndTime = i.TimeSlot?.EndTime.ToString(@"HH\:mm") ?? string.Empty,
                CourtName = i.TimeSlot?.Court?.Name ?? string.Empty
            }).ToList()
        };

        // GET /api/reservations/court/{courtId}
        [HttpGet("court/{courtId}")]
        public IActionResult GetByCourt(int courtId, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
        {
            var courtSlotIds = _uow.TimeSlots.GetByCourt(courtId)
                .Select(s => s.TimeSlotId)
                .ToHashSet();

            var reservations = _uow.Reservations.GetAll()
                .Where(r => r.Status != ReservationStatus.CANCELLED)
                .Where(r => r.ReservationItems.Any(i => courtSlotIds.Contains(i.TimeSlotId)))
                .Where(r => startDate == null || r.ReservationItems.Any(i => i.Date >= startDate))
                .Where(r => endDate == null || r.ReservationItems.Any(i => i.Date <= endDate))
                .ToList();

            return Ok(reservations.Select(MapToDto).ToList());
        }
    }
}
