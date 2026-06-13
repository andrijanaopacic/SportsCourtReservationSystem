using System;
using System.Collections.Generic;
using System.Text;

namespace Reservation.Domain.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        ISportRepository Sports { get; }
        ICourtRepository Courts { get; }

        int SaveChanges();
    }
}
