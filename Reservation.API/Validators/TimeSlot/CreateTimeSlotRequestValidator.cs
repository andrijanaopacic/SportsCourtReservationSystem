using FluentValidation;
using Reservation.API.DTOs.TimeSlot;

namespace Reservation.API.Validators.TimeSlot
{
    public class CreateTimeSlotRequestValidator : AbstractValidator<CreateTimeSlotRequest>
    {
        public CreateTimeSlotRequestValidator()
        {
            RuleFor(x => x.CourtId)
                .GreaterThan(0).WithMessage("CourtId mora biti veći od 0.");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Vreme početka je obavezno.");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("Vreme završetka je obavezno.")
                .Must((req, endTime) => endTime > req.StartTime)
                .WithMessage("Vreme završetka mora biti posle vremena početka.");
        }
    }
}
