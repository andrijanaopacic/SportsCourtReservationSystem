using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.API.DTOs.TimeSlot;
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

        public TimeSlotsController(IUnitOfWork uow, IValidator<CreateTimeSlotRequest> validator)
        {
            _uow = uow;
            _validator = validator;
        }

        // GET /api/timeslots — svi termini
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
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

            return Ok(slots);
        }

        // GET /api/timeslots/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetById(int id)
        {
            var slot = _uow.TimeSlots.GetByIdWithCourt(id);
            if (slot == null) return NotFound($"Termin sa id {id} nije pronađen.");

            return Ok(new TimeSlotDto
            {
                TimeSlotId = slot.TimeSlotId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                CourtId = slot.CourtId,
                CourtName = slot.Court?.Name ?? string.Empty
            });
        }

        // GET /api/timeslots/by-court/{courtId} — svi termini za dati teren
        [HttpGet("by-court/{courtId}")]
        [AllowAnonymous]
        public IActionResult GetByCourt(int courtId)
        {
            var court = _uow.Courts.GetById(courtId);
            if (court == null) return NotFound($"Teren sa id {courtId} nije pronađen.");

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

            return Ok(slots);
        }

        // POST /api/timeslots — samo Admin može da kreira termin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create([FromBody] CreateTimeSlotRequest request)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors
                    .Select(e => new { e.PropertyName, e.ErrorMessage }));
            }

            var court = _uow.Courts.GetById(request.CourtId);
            if (court == null) return NotFound($"Teren sa id {request.CourtId} nije pronađen.");

            // Provera da nema preklapanja termina za isti teren
            var overlapping = _uow.TimeSlots.GetByCourt(request.CourtId)
                .Any(t => t.StartTime < request.EndTime && t.EndTime > request.StartTime);

            if (overlapping)
            {
                return BadRequest("Na ovom terenu već postoji termin koji se preklapa sa zadatim vremenom.");
            }

            var slot = new TimeSlot
            {
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                CourtId = request.CourtId
            };

            _uow.TimeSlots.Add(slot);
            _uow.SaveChanges();

            return CreatedAtAction(nameof(GetById), new { id = slot.TimeSlotId }, new TimeSlotDto
            {
                TimeSlotId = slot.TimeSlotId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                CourtId = slot.CourtId,
                CourtName = court.Name
            });
        }

        // PUT /api/timeslots/{id} — samo Admin može da menja
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id, [FromBody] CreateTimeSlotRequest request)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors
                    .Select(e => new { e.PropertyName, e.ErrorMessage }));
            }

            var slot = _uow.TimeSlots.GetById(id);
            if (slot == null) return NotFound($"Termin sa id {id} nije pronađen.");

            var court = _uow.Courts.GetById(request.CourtId);
            if (court == null) return NotFound($"Teren sa id {request.CourtId} nije pronađen.");

            // Provera preklapanja (isključujemo trenutni slot)
            var overlapping = _uow.TimeSlots.GetByCourt(request.CourtId)
                .Any(t => t.TimeSlotId != id &&
                          t.StartTime < request.EndTime &&
                          t.EndTime > request.StartTime);

            if (overlapping)
            {
                return BadRequest("Na ovom terenu već postoji termin koji se preklapa sa zadatim vremenom.");
            }

            slot.StartTime = request.StartTime;
            slot.EndTime = request.EndTime;
            slot.CourtId = request.CourtId;

            _uow.TimeSlots.Update(slot);
            _uow.SaveChanges();

            return Ok(new TimeSlotDto
            {
                TimeSlotId = slot.TimeSlotId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                CourtId = slot.CourtId,
                CourtName = court.Name
            });
        }

        // DELETE /api/timeslots/{id} — samo Admin može da briše
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var slot = _uow.TimeSlots.GetById(id);
            if (slot == null) return NotFound($"Termin sa id {id} nije pronađen.");

            _uow.TimeSlots.Remove(slot);
            _uow.SaveChanges();

            return NoContent();
        }
    }
}
