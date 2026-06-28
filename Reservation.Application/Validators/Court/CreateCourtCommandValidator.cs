using FluentValidation;
using Reservation.Application.Features.Court.Commands;

namespace Reservation.Application.Validators.Court
{
    public class CreateCourtCommandValidator : AbstractValidator<CreateCourtCommand>
    {
        public CreateCourtCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Court name is required.")
                .MaximumLength(100).WithMessage("Court name must not exceed 100 characters.");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required.")
                .MaximumLength(200).WithMessage("Location must not exceed 200 characters.");

            RuleFor(x => x.PricePerHour)
                .GreaterThan(0).WithMessage("Price per hour must be greater than 0.");

            RuleFor(x => x.SportId)
                .GreaterThan(0).WithMessage("Sport is required.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
        }
    }
}