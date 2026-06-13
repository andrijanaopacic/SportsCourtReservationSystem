using FluentValidation;
using Reservation.API.DTOs.Sport;

namespace Reservation.API.Validators.Sport
{
    public class CreateSportRequestValidator : AbstractValidator<CreateSportRequest>
    {
        public CreateSportRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Sport name is required.")
                .MaximumLength(50).WithMessage("Sport name must not exceed 50 characters.");

            RuleFor(x => x.MaxPlayers)
                .GreaterThan(0).WithMessage("Maximum number of players must be greater than 0.");
                                      
        }
    }
}
