using FluentValidation;
using Reservation.API.DTOs.Auth;

namespace Reservation.API.Validators.Auth
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email je obavezan.")
                .EmailAddress().WithMessage("Email nije u ispravnom formatu.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Lozinka je obavezna.");
        }
    }
}
