using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Reservation.Commands
{
    public record UpdateReservationCommand(int Id, List<ReservationItemInput> Items) : IRequest<Result<ReservationResult?>>;

    public class UpdateReservationCommandHandler : IRequestHandler<UpdateReservationCommand, Result<ReservationResult?>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;

        private const string CacheKey = "reservations_all";
        private const string UserCacheKeyPrefix = "reservations_user_";
        private const string SingleCacheKeyPrefix = "reservation_";

        public UpdateReservationCommandHandler(IUnitOfWork uow, ICacheService cache)
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

        public async Task<Result<ReservationResult?>> Handle(UpdateReservationCommand request, CancellationToken cancellationToken)
        {
            var reservation = uow.Reservations.GetByIdWithItems(request.Id);
            if (reservation == null)
                return Result<ReservationResult?>.Fail("Reservation not found.");

            if (reservation.Status == ReservationStatus.CANCELLED)
                return Result<ReservationResult?>.Fail("Nije moguće izmeniti otkazanu rezervaciju.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            reservation.Date = today;

            var existingItems = reservation.ReservationItems.ToList();

            var requestedKeys = request.Items.Select(i => i.TimeSlotId).ToHashSet();
            var existingKeys = existingItems.Select(i => i.TimeSlotId).ToHashSet();

            var toRemove = existingItems.Where(i => !requestedKeys.Contains(i.TimeSlotId)).ToList();
            var toAdd = request.Items.Where(i => !existingKeys.Contains(i.TimeSlotId)).ToList();

            foreach (var item in toAdd)
            {
                var conflict = uow.Reservations.GetAll()
                    .Where(r => r.ReservationId != request.Id && r.Status != ReservationStatus.CANCELLED)
                    .SelectMany(r => r.ReservationItems)
                    .Any(i => i.TimeSlotId == item.TimeSlotId);

                if (conflict)
                    return Result<ReservationResult?>.Fail($"Termin {item.TimeSlotId} je već rezervisan.");
            }

            foreach (var item in toRemove)
            {
                var slot = uow.TimeSlots.GetById(item.TimeSlotId);
                if (slot != null)
                    slot.IsAvailable = true;

                reservation.ReservationItems.Remove(item);
            }

            int nextRow = reservation.ReservationItems.Any()
                ? reservation.ReservationItems.Max(i => i.RowNumber) + 1
                : 1;

            foreach (var item in toAdd)
            {
                var slot = uow.TimeSlots.GetById(item.TimeSlotId);

                if (slot == null)
                    return Result<ReservationResult?>.Fail($"TimeSlot {item.TimeSlotId} ne postoji.");

                if (slot.Date < today)
                    return Result<ReservationResult?>.Fail($"Termin za datum {slot.Date} je već prošao.");

                if (!slot.IsAvailable)
                    return Result<ReservationResult?>.Fail($"Termin {slot.TimeSlotId} nije dostupan.");

                slot.IsAvailable = false;
                reservation.ReservationItems.Add(new ReservationItem
                {
                    RowNumber = nextRow++,
                    TimeSlotId = item.TimeSlotId,
                    Price = slot.TotalPrice,
                    TimeSlot = slot
                });
            }

            if (!reservation.ReservationItems.Any())
            {
                uow.Reservations.Remove(reservation);
                uow.SaveChanges();

                await cache.RemoveAsync(CacheKey);
                await cache.RemoveAsync($"{UserCacheKeyPrefix}{reservation.ApplicationUserId}");
                await cache.RemoveAsync($"{SingleCacheKeyPrefix}{request.Id}");

                return Result<ReservationResult?>.Ok(null);
            }

            reservation.TotalPrice = reservation.ReservationItems.Sum(i => i.Price);
            reservation.Status = ReservationStatus.UPCOMING;

            uow.Reservations.Update(reservation);
            uow.SaveChanges();

            SyncStatus(reservation);

            await cache.RemoveAsync(CacheKey);
            await cache.RemoveAsync($"{UserCacheKeyPrefix}{reservation.ApplicationUserId}");
            await cache.RemoveAsync($"{SingleCacheKeyPrefix}{request.Id}");

            return Result<ReservationResult?>.Ok(CreateReservationCommandHandler.MapToResult(reservation));
        }
    }
}