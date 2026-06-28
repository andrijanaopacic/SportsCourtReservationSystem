using FluentValidation;
using Reservation.Application.Features.TimeSlot.Commands;

namespace Reservation.Application.Validators.TimeSlot
{
    public class CreateTimeSlotCommandValidator : AbstractValidator<CreateTimeSlotCommand>
    {
        public CreateTimeSlotCommandValidator()
        {
            RuleFor(x => x.CourtId)
                .GreaterThan(0).WithMessage("CourtId must be greater than 0.");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required.")
                .Must(d => d >= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Date cannot be in the past.");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Start time is required.")
                .Must(t => t.Minute == 0 && t.Second == 0)
                .WithMessage("Start time must be on a full hour (e.g. 09:00, 14:00).");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("End time is required.")
                .Must(t => t.Minute == 0 && t.Second == 0)
                .WithMessage("End time must be on a full hour (e.g. 10:00, 15:00).")
                .Must((req, endTime) => endTime > req.StartTime)
                .WithMessage("End time must be after the start time.")
                .Must((req, endTime) => (endTime - req.StartTime).TotalHours <= 8)
                .WithMessage("A single time slot cannot exceed 8 hours.");
        }
    }
}