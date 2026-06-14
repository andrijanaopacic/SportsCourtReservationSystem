using Reservation.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Domain.Repositories
{
    public interface ITimeSlotRepository : IRepository<TimeSlot>
    {
        IEnumerable<TimeSlot> GetByCourt(int courtId);
        TimeSlot? GetByIdWithCourt(int id);
    }
}
