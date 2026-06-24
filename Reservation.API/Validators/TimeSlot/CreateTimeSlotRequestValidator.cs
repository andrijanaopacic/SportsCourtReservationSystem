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

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0.")
                .LessThanOrEqualTo(100000).WithMessage("Price seems unrealistically high (max 100,000 RSD/h).");

            RuleFor(x => x)
                .Must(x => {
                    var hours = (decimal)(x.EndTime - x.StartTime).TotalHours;
                    var totalPrice = x.Price * hours;
                    return totalPrice > 0;
                })
                .WithMessage("Total price must be greater than 0.")
                .When(x => x.Price > 0 && x.EndTime > x.StartTime);
        }
    }
}