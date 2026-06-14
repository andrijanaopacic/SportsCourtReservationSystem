using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Infrastructure.Repositories
{
    public class TimeSlotRepository : Repository<TimeSlot>, ITimeSlotRepository
    {
        public TimeSlotRepository(ReservationContext context) : base(context) { }

        public IEnumerable<TimeSlot> GetByCourt(int courtId) =>
            DbSet.Include(t => t.Court)
                 .Where(t => t.CourtId == courtId)
                 .OrderBy(t => t.StartTime)
                 .ToList();

        public TimeSlot? GetByIdWithCourt(int id) =>
            DbSet.Include(t => t.Court)
                 .FirstOrDefault(t => t.TimeSlotId == id);
    }
}
