using System.Security.Claims;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services.Interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
        string GenerateRefreshToken();
        string GenerateToken(ApplicationUser user, IList<string> roles);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        bool ValidateToken(string token);
        Task<string> GenerateTokenWithRolesAsync(ApplicationUser user, IList<string> roles);
    }
}
