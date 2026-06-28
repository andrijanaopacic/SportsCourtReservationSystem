using MediatR;
using Microsoft.AspNetCore.Identity;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Infrastructure.Identity;

namespace Reservation.Application.Features.Auth.Commands
{
    public record AuthResult(string Token, string Email, string FirstName, string LastName, List<string> Roles);

    public record RegisterCommand(
        string Email, string Password, string FirstName, string LastName, string? PhoneNumber
    ) : IRequest<Result<AuthResult>>;

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResult>>
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IJwtTokenService tokenService;
        private readonly FluentValidation.IValidator<RegisterCommand> validator;

        public RegisterCommandHandler(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService tokenService,
            FluentValidation.IValidator<RegisterCommand> validator)
        {
            this.userManager = userManager;
            this.tokenService = tokenService;
            this.validator = validator;
        }

        public async Task<Result<AuthResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<AuthResult>.Fail(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing != null)
                return Result<AuthResult>.Fail("Korisnik sa ovom email adresom već postoji.");

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return Result<AuthResult>.Fail(string.Join("; ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, "Korisnik");

            var token = await tokenService.CreateTokenAsync(user);

            return Result<AuthResult>.Ok(new AuthResult(
                token, user.Email, user.FirstName, user.LastName, new List<string> { "Korisnik" }));
        }
    }
}