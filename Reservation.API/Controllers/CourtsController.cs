using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.API.DTOs.Court;
using Reservation.API.Extensions;
using Reservation.API.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CourtsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;
    private readonly IValidator<CreateCourtRequest> _validator;

    public CourtsController(IUnitOfWork uow, ICacheService cache, IValidator<CreateCourtRequest> validator)
    {
        _uow = uow;
        _cache = cache;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? name, [FromQuery] bool? isIndoor, [FromQuery] int? sportId)
    {
        var cacheKey = $"courts_{name}_{isIndoor}_{sportId}";
        var cached = await _cache.GetAsync<List<CourtDto>>(cacheKey);
        if (cached != null) return Ok(cached);

        var courts = _uow.Courts.GetAllWithSport()
            .FilterByName(name)
            .FilterByIndoor(isIndoor)
            .FilterBySport(sportId)
            .Select(c => new CourtDto
            {
                CourtId = c.CourtId,
                Name = c.Name,
                Location = c.Location,
                Description = c.Description,
                PricePerHour = c.PricePerHour,
                IsIndoor = c.IsIndoor,
                SportName = c.Sport.Name,
                SportId = c.SportId
            })
            .ToList();

        await _cache.SetAsync(cacheKey, courts);
        return Ok(courts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cacheKey = $"court_{id}";
        var cached = await _cache.GetAsync<CourtDto>(cacheKey);
        if (cached != null) return Ok(cached);

        var court = _uow.Courts.GetByIdWithSport(id);
        if (court == null) return NotFound($"Court with id {id} not found.");
        var dto = new CourtDto
        {
            CourtId = court.CourtId,
            Name = court.Name,
            Location = court.Location,
            Description = court.Description,
            PricePerHour = court.PricePerHour,
            IsIndoor = court.IsIndoor,
            SportName = court.Sport.Name,
            SportId = court.SportId  
        };

        await _cache.SetAsync(cacheKey, dto);

        return Ok(dto);
    }

    

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCourtRequest request)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var sport = _uow.Sports.GetById(request.SportId);
        if (sport == null) return NotFound($"Sport with id {request.SportId} not found.");

        var court = new Court
        {
            Name = request.Name,
            Location = request.Location,
            Description = request.Description,
            PricePerHour = request.PricePerHour,
            IsIndoor = request.IsIndoor,
            SportId = request.SportId
        };

        _uow.Courts.Add(court);
        _uow.SaveChanges();

        await _cache.RemoveByPatternAsync("sports*");
        await _cache.RemoveByPatternAsync("sport_*");
        await _cache.RemoveByPatternAsync("court_*");
        await _cache.RemoveByPatternAsync("courts*");

        return CreatedAtAction(nameof(GetById), new { id = court.CourtId }, court);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCourtRequest request)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var court = _uow.Courts.GetById(id);
        if (court == null) return NotFound($"Court with id {id} not found.");

        var sport = _uow.Sports.GetById(request.SportId);
        if (sport == null) return NotFound($"Sport with id {request.SportId} not found.");

        court.Name = request.Name;
        court.Location = request.Location;
        court.Description = request.Description;
        court.PricePerHour = request.PricePerHour;
        court.IsIndoor = request.IsIndoor;
        court.SportId = request.SportId;

        _uow.Courts.Update(court);
        _uow.SaveChanges();

        await _cache.RemoveByPatternAsync("sports*");
        await _cache.RemoveByPatternAsync("sport_*");
        await _cache.RemoveByPatternAsync("court_*");
        await _cache.RemoveByPatternAsync("courts*");

        return Ok(court);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var court = _uow.Courts.GetById(id);
        if (court == null) return NotFound($"Court with id {id} not found.");

        _uow.Courts.Remove(court);
        _uow.SaveChanges();

        await _cache.RemoveByPatternAsync("sports*");
        await _cache.RemoveByPatternAsync("sport_*");
        await _cache.RemoveByPatternAsync("court_*");
        await _cache.RemoveByPatternAsync("courts*");

        return NoContent();
    }
}