using MediatR;
using Microsoft.AspNetCore.Identity;
using Reservation.Application.Common;
using Reservation.Application.Services;
using Reservation.Infrastructure.Identity;

namespace Reservation.Application.Features.Auth.Commands
{
    public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResult>>;

    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResult>>
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IJwtTokenService tokenService;
        private readonly FluentValidation.IValidator<LoginCommand> validator;

        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService tokenService,
            FluentValidation.IValidator<LoginCommand> validator)
        {
            this.userManager = userManager;
            this.tokenService = tokenService;
            this.validator = validator;
        }

        public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return Result<AuthResult>.Fail(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Result<AuthResult>.Fail("Pogrešan email ili lozinka.");

            var roles = await userManager.GetRolesAsync(user);
            var token = await tokenService.CreateTokenAsync(user);

            return Result<AuthResult>.Ok(new AuthResult(
                token, user.Email ?? string.Empty, user.FirstName, user.LastName, roles.ToList()));
        }
    }
}