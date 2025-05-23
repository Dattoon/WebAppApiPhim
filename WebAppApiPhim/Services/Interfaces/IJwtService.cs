using System;
using System.Collections.Generic;
using System.Security.Claims;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Services
{
    public interface IJwtService
    {
        string GenerateToken(ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        bool ValidateToken(string token);
    }
}
