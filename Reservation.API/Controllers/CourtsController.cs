using Microsoft.AspNetCore.Mvc;
using Reservation.API.DTOs.Court;
using Reservation.API.Extensions;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

[Route("api/[controller]")]
[ApiController]
public class CourtsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public CourtsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] string? name, [FromQuery] bool? isIndoor)
    {
        var courts = _uow.Courts.GetAllWithSport()
            .FilterByName(name)
            .FilterByIndoor(isIndoor)
            .Select(c => new CourtDto
            {
                CourtId = c.CourtId,
                Name = c.Name,
                Location = c.Location,
                Description = c.Description,
                PricePerHour = c.PricePerHour,
                IsIndoor = c.IsIndoor,
                SportName = c.Sport.Name
            })
            .ToList();

        return Ok(courts);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var court = _uow.Courts.GetByIdWithSport(id);
        if (court == null) return NotFound($"Court with id {id} not found.");
        return Ok(new CourtDto
        {
            CourtId = court.CourtId,
            Name = court.Name,
            Location = court.Location,
            Description = court.Description,
            PricePerHour = court.PricePerHour,
            IsIndoor = court.IsIndoor,
            SportName = court.Sport.Name
        });
    }

    [HttpGet("by-sport/{sportId}")]
    public IActionResult GetBySport(int sportId)
    {
        var sport = _uow.Sports.GetById(sportId);
        if (sport == null) return NotFound($"Sport with id {sportId} not found.");

        var courts = _uow.Courts.GetBySport(sportId)
            .Select(c => new CourtDto
            {
                CourtId = c.CourtId,
                Name = c.Name,
                Location = c.Location,
                Description = c.Description,
                PricePerHour = c.PricePerHour,
                IsIndoor = c.IsIndoor,
                SportName = sport.Name
            })
            .ToList();

        return Ok(courts);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateCourtRequest request)
    {
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

        return CreatedAtAction(nameof(GetById), new { id = court.CourtId }, court);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] CreateCourtRequest request)
    {
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

        return Ok(court);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var court = _uow.Courts.GetById(id);
        if (court == null) return NotFound($"Court with id {id} not found.");

        _uow.Courts.Remove(court);
        _uow.SaveChanges();

        return NoContent();
    }
}