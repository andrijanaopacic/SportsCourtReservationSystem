using FluentValidation;
using Reservation.Application.Features.Reservation.Commands;
using Reservation.Application.Validators.Reservation;

namespace Reservation.Application.Validators.Reservation
{
    public class UpdateReservationCommandValidator : AbstractValidator<UpdateReservationCommand>
    {
        public UpdateReservationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id mora biti veći od 0.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Rezervacija mora imati barem jednu stavku.")
                .Must(items => items.Count <= 10).WithMessage("Rezervacija može imati najviše 10 stavki.");

            RuleForEach(x => x.Items)
                .SetValidator(new ReservationItemInputValidator());
        }
    }
}