using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Reservation.Commands
{
    public record CancelReservationCommand(int Id) : IRequest<Result<ReservationResult>>;

    public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, Result<ReservationResult>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        private const string CacheKey = "reservations_all";
        private const string UserCacheKeyPrefix = "reservations_user_";
        private const string SingleCacheKeyPrefix = "reservation_";

        public CancelReservationCommandHandler(IUnitOfWork uow, ICacheService cache)
        {
            this.uow = uow;
            this.cache = cache;
        }

        public async Task<Result<ReservationResult>> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
        {
            var reservation = uow.Reservations.GetByIdWithItems(request.Id);
            if (reservation == null)
                return Result<ReservationResult>.Fail("Reservation not found.");

            if (reservation.Status == ReservationStatus.CANCELLED)
                return Result<ReservationResult>.Fail("Rezervacija je već otkazana.");

            foreach (var item in reservation.ReservationItems)
            {
                var slot = uow.TimeSlots.GetById(item.TimeSlotId);
                if (slot != null)
                    slot.IsAvailable = true;
            }

            reservation.Status = ReservationStatus.CANCELLED;
            uow.Reservations.Update(reservation);
            uow.SaveChanges();

            await cache.RemoveAsync(CacheKey);
            await cache.RemoveAsync($"{UserCacheKeyPrefix}{reservation.ApplicationUserId}");
            await cache.RemoveAsync($"{SingleCacheKeyPrefix}{request.Id}");

            return Result<ReservationResult>.Ok(CreateReservationCommandHandler.MapToResult(reservation));
        }
    }
}