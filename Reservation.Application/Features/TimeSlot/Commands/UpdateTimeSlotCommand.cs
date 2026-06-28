using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Application.Features.TimeSlot.Commands;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.TimeSlot.Commands
{
    public record UpdateTimeSlotCommand(
        int Id, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, int CourtId, bool IsAvailable
    ) : IRequest<Result<TimeSlotResult>>;

    public class UpdateTimeSlotCommandHandler : IRequestHandler<UpdateTimeSlotCommand, Result<TimeSlotResult>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly FluentValidation.IValidator<UpdateTimeSlotCommand> validator;

        public UpdateTimeSlotCommandHandler(IUnitOfWork uow, ICacheService cache, FluentValidation.IValidator<UpdateTimeSlotCommand> validator)
        {
            this.uow = uow;
            this.cache = cache;
            this.validator = validator;
        }

        private static decimal ComputeTotalPrice(decimal price, TimeOnly start, TimeOnly end)
        {
            var hours = (decimal)(end - start).TotalHours;
            return hours > 0 ? price * hours : 0;
        }

        public async Task<Result<TimeSlotResult>> Handle(UpdateTimeSlotCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<TimeSlotResult>.Fail(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var slot = uow.TimeSlots.GetById(request.Id);
            if (slot == null)
                return Result<TimeSlotResult>.Fail($"Time slot with ID {request.Id} was not found.");

            var court = uow.Courts.GetById(request.CourtId);
            if (court == null)
                return Result<TimeSlotResult>.Fail($"Court with ID {request.CourtId} was not found.");

            var overlapping = uow.TimeSlots.GetByCourt(request.CourtId)
                .Any(t => t.TimeSlotId != request.Id &&
                          t.Date == request.Date &&
                          t.StartTime < request.EndTime &&
                          t.EndTime > request.StartTime);

            if (overlapping)
                return Result<TimeSlotResult>.Fail("An overlapping time slot already exists on this court for that date.");

            var oldCourtId = slot.CourtId;

            slot.Date = request.Date;
            slot.StartTime = request.StartTime;
            slot.EndTime = request.EndTime;
            slot.Duration = request.EndTime - request.StartTime;
            slot.Price = court.PricePerHour;
            slot.TotalPrice = ComputeTotalPrice(court.PricePerHour, request.StartTime, request.EndTime);
            slot.IsAvailable = request.IsAvailable;
            slot.CourtId = request.CourtId;

            uow.TimeSlots.Update(slot);
            uow.SaveChanges();

            await cache.RemoveByPatternAsync("timeslots:all:*");
            await cache.RemoveAsync($"timeslots:id:{request.Id}");
            await cache.RemoveAsync($"timeslots:court:{request.CourtId}");
            await cache.RemoveAsync("timeslots:available");
            if (oldCourtId != request.CourtId)
                await cache.RemoveAsync($"timeslots:court:{oldCourtId}");

            var result = new TimeSlotResult(
                slot.TimeSlotId, slot.Date, slot.StartTime, slot.EndTime,
                slot.Duration, slot.Price, slot.TotalPrice, slot.IsAvailable,
                slot.CourtId, court.Name);

            return Result<TimeSlotResult>.Ok(result);
        }
    }
}