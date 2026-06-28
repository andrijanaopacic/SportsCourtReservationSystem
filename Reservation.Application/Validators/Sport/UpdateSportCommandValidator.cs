using FluentValidation;
using Reservation.Application.Features.Sport.Commands;

namespace Reservation.Application.Validators.Sport
{
    public class UpdateSportCommandValidator : AbstractValidator<UpdateSportCommand>
    {
        public UpdateSportCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Sport id must be greater than 0.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Sport name is required.")
                .MaximumLength(50).WithMessage("Sport name must not exceed 50 characters.");

            RuleFor(x => x.MaxPlayers)
                .GreaterThan(0).WithMessage("Maximum number of players must be greater than 0.");
        }
    }
}