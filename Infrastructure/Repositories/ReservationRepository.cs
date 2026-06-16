using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Infrastructure.Repositories
{
    public class ReservationRepository : Repository<ReservationEntity>, IReservationRepository
    {
        public ReservationRepository(ReservationContext ctx) : base(ctx) { }

        public new IEnumerable<ReservationEntity> GetAll() =>
            Context.Reservations
                .Include(r => r.ReservationItems)
                    .ThenInclude(i => i.TimeSlot)
                        .ThenInclude(t => t.Court)
                .ToList();

        public IEnumerable<ReservationEntity> GetByUser(string userId) =>
            Context.Reservations
                .Include(r => r.ReservationItems)
                    .ThenInclude(i => i.TimeSlot)
                        .ThenInclude(t => t.Court)
                .Where(r => r.ApplicationUserId == userId);

        public IEnumerable<ReservationEntity> GetByStatus(ReservationStatus status) =>
            Context.Reservations
                .Include(r => r.ReservationItems)
                    .ThenInclude(i => i.TimeSlot)
                        .ThenInclude(t => t.Court)
                .Where(r => r.Status == status);

        public ReservationEntity? GetByIdWithItems(int id) =>
            Context.Reservations
                .Include(r => r.ReservationItems)
                    .ThenInclude(i => i.TimeSlot)
                        .ThenInclude(t => t.Court)
                .FirstOrDefault(r => r.ReservationId == id);
    }
}

