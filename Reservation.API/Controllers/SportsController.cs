using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reservation.API.DTOs.Court;
using Reservation.API.DTOs.Sport;
using Reservation.API.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SportsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ICacheService _cache;
        private readonly IValidator<CreateSportRequest> _validator;

        public SportsController(IUnitOfWork uow, ICacheService cache, IValidator<CreateSportRequest> validator)
        {
            _uow = uow;
            _cache = cache;
            _validator = validator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? name)
        {
            var cacheKey = $"sports_{name}";
            var cached = await _cache.GetAsync<List<Sport>>(cacheKey);
            if(cached != null) return Ok(cached);

            var sports = _uow.Sports.SearchByName(name);

            await _cache.SetAsync(cacheKey, sports);

            return Ok(sports);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cacheKey = $"sport_{id}";
            var cached = await _cache.GetAsync<SportDetailsDto>(cacheKey);
            if (cached != null) return Ok(cached);

            var sport = _uow.Sports.GetByIdWithCourts(id);
            if (sport == null) return NotFound($"Sport with id {id} not found.");

            var dto = new SportDetailsDto
            {
                SportId = sport.SportId,
                Name = sport.Name,
                MaxPlayers = sport.MaxPlayers,
                Courts = sport.Courts.Select(c => new CourtDto
                {
                    CourtId = c.CourtId,
                    Name = c.Name,
                    Location = c.Location,
                    Description = c.Description,
                    PricePerHour = c.PricePerHour,
                    IsIndoor = c.IsIndoor,
                    SportId = c.SportId,
                    SportName = sport.Name
                }).ToList()
            };

            await _cache.SetAsync(cacheKey, dto);

            return Ok(dto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateSportRequest request)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

            var existing = _uow.Sports.GetByName(request.Name);
            if (existing != null) return BadRequest("Sport with this name already exists.");

            var sport = new Sport
            {
                Name = request.Name,
                MaxPlayers = request.MaxPlayers
            };

            _uow.Sports.Add(sport);
            _uow.SaveChanges();

            await _cache.RemoveByPatternAsync("sports*");
            await _cache.RemoveByPatternAsync("sport_*");  
            await _cache.RemoveByPatternAsync("court_*");  
            await _cache.RemoveByPatternAsync("courts*");

            return CreatedAtAction(nameof(GetById), new { id = sport.SportId }, sport);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSportRequest request)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

            var sport = _uow.Sports.GetById(id);
            if (sport == null) return NotFound($"Sport with id {id} not found.");

            sport.Name = request.Name;
            sport.MaxPlayers = request.MaxPlayers;

            _uow.Sports.Update(sport);
            _uow.SaveChanges();

            await _cache.RemoveAsync($"sport_{id}");
            await _cache.RemoveByPatternAsync("sports*");
            await _cache.RemoveByPatternAsync("sport_*");
            await _cache.RemoveByPatternAsync("court_*");
            await _cache.RemoveByPatternAsync("courts*");

            return Ok(sport);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var sport = _uow.Sports.GetByIdWithCourts(id);
            if (sport == null) return NotFound($"Sport with id {id} not found.");

            if (sport.Courts.Any())
                return BadRequest("Cannot delete sport because it has courts assigned to it. Please delete the courts first.");

            _uow.Sports.Remove(sport);
            _uow.SaveChanges();

            await _cache.RemoveAsync($"sport_{id}");
            await _cache.RemoveByPatternAsync("sports*");
            await _cache.RemoveByPatternAsync("sport_*");
            await _cache.RemoveByPatternAsync("court_*");
            await _cache.RemoveByPatternAsync("courts*");

            return NoContent();
        }
    }
}
