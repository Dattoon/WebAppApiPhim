using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IJwtService
    {
        string GenerateJwtToken(ApplicationUser user, IList<string> roles);
        string ValidateJwtToken(string token);
    }
}
