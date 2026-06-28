using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Reservation.Queries
{
    public record GetReservationByIdQuery(int Id) : IRequest<Result<ReservationResult>>;

    public class GetReservationByIdQueryHandler : IRequestHandler<GetReservationByIdQuery, Result<ReservationResult>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        private const string SingleCacheKeyPrefix = "reservation_";

        public GetReservationByIdQueryHandler(IUnitOfWork uow, ICacheService cache)
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

        public async Task<Result<ReservationResult>> Handle(GetReservationByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{SingleCacheKeyPrefix}{request.Id}";

            var cached = await cache.GetAsync<ReservationResult>(cacheKey);
            if (cached != null)
                return Result<ReservationResult>.Ok(cached);

            var reservation = uow.Reservations.GetByIdWithItems(request.Id);
            if (reservation == null)
                return Result<ReservationResult>.Fail("Reservation not found.");

            SyncStatus(reservation);

            var result = CreateReservationCommandHandler.MapToResult(reservation);
            await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Result<ReservationResult>.Ok(result);
        }
    }
}