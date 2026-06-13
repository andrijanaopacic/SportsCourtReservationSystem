using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reservation.API.DTOs.Sport;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SportsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public SportsController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public IActionResult GetAll([FromQuery] string? name)
        {
            var sports = _uow.Sports.SearchByName(name);
            return Ok(sports);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var sport = _uow.Sports.GetByIdWithCourts(id);
            if (sport == null) return NotFound($"Sport with id {id} not found.");
            return Ok(sport);
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateSportRequest request)
        {
            var existing = _uow.Sports.GetByName(request.Name);
            if (existing != null) return BadRequest("Sport with this name already exists.");

            var sport = new Sport
            {
                Name = request.Name,
                MaxPlayers = request.MaxPlayers
            };

            _uow.Sports.Add(sport);
            _uow.SaveChanges();

            return CreatedAtAction(nameof(GetById), new { id = sport.SportId }, sport);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] CreateSportRequest request)
        {
            var sport = _uow.Sports.GetById(id);
            if (sport == null) return NotFound($"Sport with id {id} not found.");

            sport.Name = request.Name;
            sport.MaxPlayers = request.MaxPlayers;

            _uow.Sports.Update(sport);
            _uow.SaveChanges();

            return Ok(sport);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var sport = _uow.Sports.GetByIdWithCourts(id);
            if (sport == null) return NotFound($"Sport with id {id} not found.");

            if (sport.Courts.Any())
                return BadRequest("Cannot delete sport because it has courts assigned to it. Please delete the courts first.");

            _uow.Sports.Remove(sport);
            _uow.SaveChanges();

            return NoContent();
        }
    }
}
