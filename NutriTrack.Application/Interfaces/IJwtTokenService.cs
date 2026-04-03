using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(int userId, string role);
        string GenerateRefreshToken();
    }
}
