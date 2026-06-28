using FluentValidation;
using Reservation.Application.Features.Sport.Commands;

namespace Reservation.Application.Validators.Sport
{
    public class CreateSportCommandValidator : AbstractValidator<CreateSportCommand>
    {
        public CreateSportCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Sport name is required.")
                .MaximumLength(50).WithMessage("Sport name must not exceed 50 characters.");

            RuleFor(x => x.MaxPlayers)
                .GreaterThan(0).WithMessage("Maximum number of players must be greater than 0.");
        }
    }
}