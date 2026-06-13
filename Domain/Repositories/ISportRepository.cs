using Reservation.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Domain.Repositories
{
    public interface ISportRepository : IRepository<Sport>
    {
        Sport? GetByIdWithCourts(int id);

        Sport? GetByName(string name);
        IEnumerable<Sport> GetAllWithCourts();
        IEnumerable<Sport> SearchByName(string? name);
    }
}
