using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Models;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.Reservation.Commands
{
    public record ReservationItemInput(int TimeSlotId);

    public record ReservationItemResult(int RowNumber, decimal Price, DateOnly Date, int TimeSlotId, string StartTime, string EndTime, string CourtName);

    public record ReservationResult(int ReservationId, string Status, decimal TotalPrice, string ApplicationUserId, DateOnly Date, List<ReservationItemResult> Items);

    public record CreateReservationCommand(string UserId, List<ReservationItemInput> Items) : IRequest<Result<ReservationResult>>;

    public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, Result<ReservationResult>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly FluentValidation.IValidator<CreateReservationCommand> validator;

        private const string CacheKey = "reservations_all";
        private const string UserCacheKeyPrefix = "reservations_user_";

        public CreateReservationCommandHandler(IUnitOfWork uow, ICacheService cache, FluentValidation.IValidator<CreateReservationCommand> validator)
        {
            this.uow = uow;
            this.cache = cache;
            this.validator = validator;
        }

        public static ReservationResult MapToResult(Domain.Models.ReservationEntity r) => new(
            r.ReservationId, r.Status.ToString(), r.TotalPrice, r.ApplicationUserId, r.Date,
            r.ReservationItems.Select(i => new ReservationItemResult(
                i.RowNumber, i.TimeSlot.TotalPrice, i.TimeSlot.Date, i.TimeSlotId,
                i.TimeSlot?.StartTime.ToString(@"HH\:mm") ?? string.Empty,
                i.TimeSlot?.EndTime.ToString(@"HH\:mm") ?? string.Empty,
                i.TimeSlot?.Court?.Name ?? string.Empty
            )).ToList());

        public async Task<Result<ReservationResult>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<ReservationResult>.Fail(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var item in request.Items)
            {
                var conflict = uow.Reservations.GetAll()
                    .Where(r => r.Status != Domain.Models.ReservationStatus.CANCELLED)
                    .SelectMany(r => r.ReservationItems)
                    .Any(i => i.TimeSlotId == item.TimeSlotId);

                if (conflict)
                    return Result<ReservationResult>.Fail($"Termin {item.TimeSlotId} je već rezervisan.");
            }

            var items = new List<ReservationItem>();
            int rowNumber = 1;

            foreach (var item in request.Items)
            {
                var slot = uow.TimeSlots.GetById(item.TimeSlotId);

                if (slot == null)
                    return Result<ReservationResult>.Fail($"TimeSlot {item.TimeSlotId} ne postoji.");

                if (slot.Date < today)
                    return Result<ReservationResult>.Fail($"Termin za datum {slot.Date} je već prošao.");

                if (!slot.IsAvailable)
                    return Result<ReservationResult>.Fail($"Termin {slot.TimeSlotId} nije dostupan.");

                slot.IsAvailable = false;

                items.Add(new ReservationItem
                {
                    RowNumber = rowNumber++,
                    TimeSlotId = slot.TimeSlotId,
                    Price = slot.TotalPrice,
                    TimeSlot = slot
                });
            }

            var dates = items.Select(i => i.TimeSlot.Date).ToList();
            Domain.Models.ReservationStatus initialStatus;
            if (dates.All(d => d < today))
                initialStatus = Domain.Models.ReservationStatus.COMPLETED;
            else if (dates.Any(d => d == today))
                initialStatus = Domain.Models.ReservationStatus.ACTIVE;
            else
                initialStatus = Domain.Models.ReservationStatus.UPCOMING;

            var reservation = new Domain.Models.ReservationEntity
            {
                ApplicationUserId = request.UserId,
                Status = initialStatus,
                ReservationItems = items,
                TotalPrice = items.Sum(i => i.Price),
                Date = today
            };

            uow.Reservations.Add(reservation);
            uow.SaveChanges();

            await cache.RemoveAsync(CacheKey);
            await cache.RemoveAsync($"{UserCacheKeyPrefix}{request.UserId}");

            return Result<ReservationResult>.Ok(MapToResult(reservation));
        }
    }
}