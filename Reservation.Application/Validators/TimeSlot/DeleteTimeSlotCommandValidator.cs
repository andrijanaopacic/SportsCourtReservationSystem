using FluentValidation;
using Reservation.Application.Features.TimeSlot.Commands;

namespace Reservation.Application.Validators.TimeSlot
{
    public class DeleteTimeSlotCommandValidator : AbstractValidator<DeleteTimeSlotCommand>
    {
        public DeleteTimeSlotCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}