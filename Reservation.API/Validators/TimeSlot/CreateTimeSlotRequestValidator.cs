using FluentValidation;
using Reservation.API.DTOs.TimeSlot;

namespace Reservation.API.Validators.TimeSlot
{
    public class CreateTimeSlotRequestValidator : AbstractValidator<CreateTimeSlotRequest>
    {
        public CreateTimeSlotRequestValidator()
        {
            RuleFor(x => x.CourtId)
                .GreaterThan(0).WithMessage("CourtId must be greater than 0.");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Start time is required.");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("End time is required.")
                .Must((req, endTime) => endTime > req.StartTime)
                .WithMessage("End time must be after the start time.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
        }
    }
}