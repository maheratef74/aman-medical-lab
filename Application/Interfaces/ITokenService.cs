using DrMohamedWeb.Core.Entities;

namespace DrMohamedWeb.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(string username, string role = "Admin");
        string GenerateRefreshToken();
        Task<RefreshToken> SaveRefreshTokenAsync(string username, string token);
        Task<(string newAccessToken, string newRefreshToken)?> RefreshTokensAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string token);
    }
}
