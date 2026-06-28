using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.TimeSlot.Commands
{
    public record TimeSlotResult(
        int TimeSlotId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime,
        TimeSpan Duration, decimal Price, decimal TotalPrice, bool IsAvailable,
        int CourtId, string CourtName);

    public record CreateTimeSlotCommand(
        DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, int CourtId
    ) : IRequest<Result<TimeSlotResult>>;

    public class CreateTimeSlotCommandHandler : IRequestHandler<CreateTimeSlotCommand, Result<TimeSlotResult>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly FluentValidation.IValidator<CreateTimeSlotCommand> validator;

        public CreateTimeSlotCommandHandler(IUnitOfWork uow, ICacheService cache, FluentValidation.IValidator<CreateTimeSlotCommand> validator)
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

        public async Task<Result<TimeSlotResult>> Handle(CreateTimeSlotCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<TimeSlotResult>.Fail(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var court = uow.Courts.GetById(request.CourtId);
            if (court == null)
                return Result<TimeSlotResult>.Fail($"Court with ID {request.CourtId} was not found.");

            var overlapping = uow.TimeSlots.GetByCourt(request.CourtId)
                .Any(t => t.Date == request.Date &&
                          t.StartTime < request.EndTime &&
                          t.EndTime > request.StartTime);

            if (overlapping)
                return Result<TimeSlotResult>.Fail("An overlapping time slot already exists on this court for that date.");

            var slot = new Domain.Models.TimeSlot
            {
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Duration = request.EndTime - request.StartTime,
                Price = court.PricePerHour,
                TotalPrice = ComputeTotalPrice(court.PricePerHour, request.StartTime, request.EndTime),
                IsAvailable = true,
                CourtId = request.CourtId
            };

            uow.TimeSlots.Add(slot);
            uow.SaveChanges();

            await cache.RemoveByPatternAsync("timeslots:all:*");
            await cache.RemoveAsync($"timeslots:court:{request.CourtId}");
            await cache.RemoveAsync("timeslots:available");

            var result = new TimeSlotResult(
                slot.TimeSlotId, slot.Date, slot.StartTime, slot.EndTime,
                slot.Duration, slot.Price, slot.TotalPrice, slot.IsAvailable,
                slot.CourtId, court.Name);

            return Result<TimeSlotResult>.Ok(result);
        }
    }
}