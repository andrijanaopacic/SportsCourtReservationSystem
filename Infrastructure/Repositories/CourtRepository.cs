using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Infrastructure.Repositories
{
    public class CourtRepository : Repository<Court>, ICourtRepository
    {
        public CourtRepository(ReservationContext context) : base(context) { }

        public Court? GetByIdWithSport(int id) =>
            DbSet.Include(c => c.Sport)
                 .FirstOrDefault(c => c.CourtId == id);

        public IEnumerable<Court> GetAllWithSport() =>
            DbSet.Include(c => c.Sport).ToList();

    }
}
