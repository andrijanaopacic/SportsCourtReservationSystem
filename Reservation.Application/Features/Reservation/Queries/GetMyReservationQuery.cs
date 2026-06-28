using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Reservation.Queries
{
    public record GetMyReservationsQuery(string UserId) : IRequest<Result<List<ReservationResult>>>;

    public class GetMyReservationsQueryHandler : IRequestHandler<GetMyReservationsQuery, Result<List<ReservationResult>>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        private const string UserCacheKeyPrefix = "reservations_user_";

        public GetMyReservationsQueryHandler(IUnitOfWork uow, ICacheService cache)
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

        public async Task<Result<List<ReservationResult>>> Handle(GetMyReservationsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{UserCacheKeyPrefix}{request.UserId}";

            var cached = await cache.GetAsync<List<ReservationResult>>(cacheKey);
            if (cached != null)
                return Result<List<ReservationResult>>.Ok(cached);

            var reservations = uow.Reservations.GetByUser(request.UserId).ToList();
            foreach (var r in reservations) SyncStatus(r);

            var result = reservations.Select(CreateReservationCommandHandler.MapToResult).ToList();
            await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Result<List<ReservationResult>>.Ok(result);
        }
    }
}