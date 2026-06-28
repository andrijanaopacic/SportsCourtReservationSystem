using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Reservation.Queries
{
    public record GetAllReservationsQuery(string? Status) : IRequest<Result<List<ReservationResult>>>;

    public class GetAllReservationsQueryHandler : IRequestHandler<GetAllReservationsQuery, Result<List<ReservationResult>>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        private const string CacheKey = "reservations_all";

        public GetAllReservationsQueryHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        private void SyncStatus(ReservationEntity r)
        {
            if (r.Status == ReservationStatus.CANCELLED) return;
            if (!r.ReservationItems.Any()) return;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var dates = r.ReservationItems.Select(i => i.TimeSlot.Date).ToList();

            ReservationStatus updated;
            if (dates.All(d => d < today))
                updated = ReservationStatus.COMPLETED;
            else if (dates.Any(d => d == today))
                updated = ReservationStatus.ACTIVE;
            else
                updated = ReservationStatus.UPCOMING;

            if (r.Status != updated)
            {
                r.Status = updated;
                uow.Reservations.Update(r);
                uow.SaveChanges();
            }
        }

        public async Task<Result<List<ReservationResult>>> Handle(GetAllReservationsQuery request, CancellationToken cancellationToken)
        {
            if (request.Status == null)
            {
                var cached = await cache.GetAsync<List<ReservationResult>>(CacheKey);
                if (cached != null)
                    return Result<List<ReservationResult>>.Ok(cached);
            }

            var all = uow.Reservations.GetAll().ToList();
            foreach (var r in all) SyncStatus(r);

            var filtered = request.Status != null && Enum.TryParse<ReservationStatus>(request.Status, out var s)
                ? all.Where(r => r.Status == s)
                : all;

            var result = filtered.Select(CreateReservationCommandHandler.MapToResult).ToList();

            if (request.Status == null)
                await cache.SetAsync(CacheKey, result, TimeSpan.FromMinutes(5));

            return Result<List<ReservationResult>>.Ok(result);
        }
    }
}