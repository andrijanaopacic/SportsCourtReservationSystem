using Reservation.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Domain.Repositories
{
    public interface IReservationRepository : IRepository<ReservationEntity>
    {
        IEnumerable<ReservationEntity> GetByUser(string userId);
        IEnumerable<ReservationEntity> GetByStatus(ReservationStatus status);
        ReservationEntity? GetByIdWithItems(int id);
    }
}
