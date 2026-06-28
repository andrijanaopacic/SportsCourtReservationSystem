using FluentValidation;
using Reservation.Application.Features.Court.Commands;

namespace Reservation.Application.Validators.Court
{
    public class DeleteCourtCommandValidator : AbstractValidator<DeleteCourtCommand>
    {
        public DeleteCourtCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Court id must be greater than 0.");
        }
    }
}