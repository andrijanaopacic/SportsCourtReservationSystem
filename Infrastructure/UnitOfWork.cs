using Reservation.Domain.Repositories;
using Reservation.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ReservationContext _context;

        private ISportRepository? _sports;
        private ICourtRepository? _courts;
        private ITimeSlotRepository? _timeSlots;

        public UnitOfWork(ReservationContext context)
        {
            _context = context;
        }
        public ISportRepository Sports => _sports ??= new SportRepository(_context);
        public ICourtRepository Courts => _courts ??= new CourtRepository(_context);
        public ITimeSlotRepository TimeSlots => _timeSlots ??= new TimeSlotRepository(_context);

        public void Dispose() =>  _context.Dispose();

        public int SaveChanges() => _context.SaveChanges();
        
    }
}
