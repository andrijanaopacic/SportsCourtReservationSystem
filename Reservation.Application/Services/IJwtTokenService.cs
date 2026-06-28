using Reservation.Infrastructure.Identity;

namespace Reservation.Application.Services
{
    public interface IJwtTokenService
    {
        Task<string> CreateTokenAsync(ApplicationUser user);
    }
}