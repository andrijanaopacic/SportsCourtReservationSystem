using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Infrastructure.Repositories
{
    public class SportRepository : Repository<Sport>, ISportRepository
    {
        public SportRepository(ReservationContext context) : base(context) { }

        public Sport? GetByIdWithCourts(int id) =>
            DbSet.Include(s => s.Courts)
                 .FirstOrDefault(s => s.SportId == id);

        public Sport? GetByName(string name) =>
            DbSet.FirstOrDefault(s => s.Name.ToLower() == name.ToLower());

        public IEnumerable<Sport> GetAllWithCourts() =>
            DbSet.Include(s => s.Courts).ToList();
        public IEnumerable<Sport> SearchByName(string? name) =>
            DbSet.Where(s => string.IsNullOrWhiteSpace(name) || s.Name.Contains(name))
                 .ToList();
    }
}
