using FluentValidation;
using Reservation.API.DTOs.Auth;

namespace Reservation.API.Validators.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email je obavezan.")
                .EmailAddress().WithMessage("Email nije u ispravnom formatu.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Lozinka je obavezna.")
                .MinimumLength(5).WithMessage("Lozinka mora imati najmanje 5 karaktera.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Lozinka mora sadržati bar jedan specijalni karakter.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ime je obavezno.")
                .MaximumLength(50).WithMessage("Ime ne može imati više od 50 karaktera.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Prezime je obavezno.")
                .MaximumLength(50).WithMessage("Prezime ne može imati više od 50 karaktera.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Broj telefona je obavezan.")
                .Matches(@"^[0-9+\s-]+$").WithMessage("Broj telefona nije u ispravnom formatu.")
                .MaximumLength(20).WithMessage("Broj telefona ne može imati više od 20 karaktera.");
        }
    }
}
