using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Reservation.API.DTOs.Reservation;
using Reservation.API.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;

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

        // GET /api/reservations?status=UPCOMING
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            // Pokušaj iz keša (samo ako nema filtera po statusu)
            if (status == null)
            {
                var cached = await _cache.GetAsync<List<ReservationDto>>(CacheKey);
                if (cached != null)
                    return Ok(cached);
            }

            var reservations = status != null && Enum.TryParse<ReservationStatus>(status, out var s)
                ? _uow.Reservations.GetByStatus(s)
                : _uow.Reservations.GetAll();

            var dtos = reservations.Select(MapToDto).ToList();

            if (status == null)
                await _cache.SetAsync(CacheKey, dtos, TimeSpan.FromMinutes(5));

            return Ok(dtos);
        }

        // GET /api/reservations/my — rezervacije ulogovanog korisnika
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cacheKey = $"{UserCacheKeyPrefix}{userId}";

            var cached = await _cache.GetAsync<List<ReservationDto>>(cacheKey);
            if (cached != null)
                return Ok(cached);

            var reservations = _uow.Reservations.GetByUser(userId);
            var dtos = reservations.Select(MapToDto).ToList();

            await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5));
            return Ok(dtos);
        }

        // GET /api/reservations/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var reservation = _uow.Reservations.GetByIdWithItems(id);
            if (reservation == null) return NotFound();
            return Ok(MapToDto(reservation));
        }

        // POST /api/reservations
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
        {
            // Konflikt: termin već rezervisan za isti datum
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

            var reservation = new ReservationEntity
            {
                ApplicationUserId = userId,
                Status = ReservationStatus.UPCOMING,
                ReservationItems = request.Items.Select((item, idx) => new ReservationItem
                {
                    RowNumber = idx + 1,
                    TimeSlotId = item.TimeSlotId,
                    Date = item.Date,
                    Price = _uow.TimeSlots.GetById(item.TimeSlotId)?.Price ?? 0
                }).ToList()
            };

            reservation.TotalPrice = reservation.ReservationItems.Sum(i => i.Price);
            _uow.Reservations.Add(reservation);
            _uow.SaveChanges();

            // Invalidacija keša
            await _cache.RemoveAsync(CacheKey);
            await _cache.RemoveAsync($"{UserCacheKeyPrefix}{userId}");

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
                TimeSlotId = i.TimeSlotId
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
