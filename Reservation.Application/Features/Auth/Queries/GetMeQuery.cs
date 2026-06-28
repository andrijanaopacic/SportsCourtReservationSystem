using MediatR;
using Microsoft.AspNetCore.Identity;
using Reservation.Application.Common;
using Reservation.Infrastructure.Identity;

namespace Reservation.Application.Features.Auth.Queries
{
    public record UserResult(string Id, string Email, string FirstName, string LastName, string PhoneNumber, List<string> Roles);

    public record GetMeQuery(string UserId) : IRequest<Result<UserResult>>;

    public class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<UserResult>>
    {
        private readonly UserManager<ApplicationUser> userManager;

        public GetMeQueryHandler(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        public async Task<Result<UserResult>> Handle(GetMeQuery request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return Result<UserResult>.Fail("User not found.");

            var roles = await userManager.GetRolesAsync(user);

            return Result<UserResult>.Ok(new UserResult(
                user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName,
                user.PhoneNumber ?? string.Empty, roles.ToList()));
        }
    }
}