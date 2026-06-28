using FluentValidation;
using Reservation.Application.Features.Reservation.Commands;

namespace Reservation.Application.Validators.Reservation
{
    public class ReservationItemInputValidator : AbstractValidator<ReservationItemInput>
    {
        public ReservationItemInputValidator()
        {
            RuleFor(x => x.TimeSlotId)
                .GreaterThan(0).WithMessage("TimeSlotId mora biti veći od 0.");
        }
    }

    public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
    {
        public CreateReservationCommandValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Rezervacija mora imati barem jednu stavku.")
                .Must(items => items.Count <= 10).WithMessage("Rezervacija može imati najviše 10 stavki.");

            RuleForEach(x => x.Items)
                .SetValidator(new ReservationItemInputValidator());

            RuleFor(x => x.Items)
                .Must(items =>
                {
                    var duplicates = items
                        .GroupBy(i => i.TimeSlotId)
                        .Any(g => g.Count() > 1);
                    return !duplicates;
                })
                .WithMessage("Rezervacija sadrži duplikate: isti termin na isti datum.");
        }
    }
}