using FluentValidation;
using Reservation.Application.Features.Sport.Commands;

namespace Reservation.Application.Validators.Sport
{
    public class DeleteSportCommandValidator : AbstractValidator<DeleteSportCommand>
    {
        public DeleteSportCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Sport id must be greater than 0.");
        }
    }
}