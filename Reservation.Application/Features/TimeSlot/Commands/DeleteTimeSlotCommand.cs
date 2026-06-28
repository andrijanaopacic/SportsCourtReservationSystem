using MediatR;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Domain.Repositories;

namespace Reservation.Application.Features.TimeSlot.Commands
{
    public record DeleteTimeSlotCommand(int Id) : IRequest<Result<bool>>;

    public class DeleteTimeSlotCommandHandler : IRequestHandler<DeleteTimeSlotCommand, Result<bool>>
    {
        private readonly IUnitOfWork uow;
        private readonly ICacheService cache;
        private readonly FluentValidation.IValidator<DeleteTimeSlotCommand> validator;

        public DeleteTimeSlotCommandHandler(IUnitOfWork uow, ICacheService cache, FluentValidation.IValidator<DeleteTimeSlotCommand> validator)
        {
            this.uow = uow;
            this.cache = cache;
            this.validator = validator;
        }

        public async Task<Result<bool>> Handle(DeleteTimeSlotCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<bool>.Fail(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var slot = uow.TimeSlots.GetById(request.Id);
            if (slot == null)
                return Result<bool>.Fail($"Time slot with ID {request.Id} was not found.");

            var courtId = slot.CourtId;

            uow.TimeSlots.Remove(slot);
            uow.SaveChanges();

            await cache.RemoveByPatternAsync("timeslots:all:*");
            await cache.RemoveAsync($"timeslots:id:{request.Id}");
            await cache.RemoveAsync($"timeslots:court:{courtId}");
            await cache.RemoveAsync("timeslots:available");

            return Result<bool>.Ok(true);
        }
    }
}