using FluentValidation;
using Reservation.API.DTOs.Reservation;

namespace Reservation.API.Validators.Reservation
{
    public class CreateReservationItemRequestValidator : AbstractValidator<CreateReservationItemRequest>
    {
        public CreateReservationItemRequestValidator()
        {
            RuleFor(x => x.TimeSlotId)
                .GreaterThan(0).WithMessage("TimeSlotId mora biti veći od 0.");

            RuleFor(x => x.Date)
                .Must(d => d >= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Datum rezervacije ne može biti u prošlosti.");
        }
    }

    public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
    {
        public CreateReservationRequestValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Rezervacija mora imati barem jednu stavku.")
                .Must(items => items.Count <= 10).WithMessage("Rezervacija može imati najviše 10 stavki.");

            RuleForEach(x => x.Items)
                .SetValidator(new CreateReservationItemRequestValidator());

            RuleFor(x => x.Items)
                .Must(items =>
                {
                    var duplicates = items
                        .GroupBy(i => new { i.TimeSlotId, i.Date })
                        .Any(g => g.Count() > 1);
                    return !duplicates;
                })
                .WithMessage("Rezervacija sadrži duplikate: isti termin na isti datum.");
        }
    }


}

