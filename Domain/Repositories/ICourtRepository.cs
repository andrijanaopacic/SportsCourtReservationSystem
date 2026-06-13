using Reservation.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Domain.Repositories
{
    public interface ICourtRepository : IRepository<Court>
    {
        Court? GetByIdWithSport(int id);
        IEnumerable<Court> GetBySport(int sportId);
        IEnumerable<Court> GetAllWithSport();
    }
}
