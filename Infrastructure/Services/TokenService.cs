using DrMohamedWeb.Application.Interfaces;
using DrMohamedWeb.Core.Entities;
using DrMohamedWeb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DrMohamedWeb.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly AmanDbContext _context;
        private readonly IConfiguration _config;

        public TokenService(AmanDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public string GenerateAccessToken(string username, string role = "Admin")
        {
            var secretKey = _config["Jwt:SecretKey"] ?? "AmanMedicalLabSecretKeyForJwtAuthentication2026!";
            var issuer = _config["Jwt:Issuer"] ?? "AmanLabApp";
            var audience = _config["Jwt:Audience"] ?? "AmanLabUsers";
            var minutes = int.TryParse(_config["Jwt:AccessTokenExpirationMinutes"], out var m) ? m : 15;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<RefreshToken> SaveRefreshTokenAsync(string username, string token)
        {
            var days = int.TryParse(_config["Jwt:RefreshTokenExpirationDays"], out var d) ? d : 7;

            var refreshToken = new RefreshToken
            {
                Username = username,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(days),
                IsRevoked = false
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<(string username, string newAccessToken, string newRefreshToken)?> RefreshTokensAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var existingToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked);

            if (existingToken == null || existingToken.ExpiresAt <= DateTime.UtcNow)
                return null;

            // Revoke current refresh token
            existingToken.IsRevoked = true;
            existingToken.RevokedAt = DateTime.UtcNow;

            // Generate new pair
            var username = existingToken.Username;
            var newAccessToken = GenerateAccessToken(username);
            var newRefreshToken = GenerateRefreshToken();

            await SaveRefreshTokenAsync(username, newRefreshToken);
            await _context.SaveChangesAsync();

            return (username, newAccessToken, newRefreshToken);
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var existingToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == token);

            if (existingToken != null && !existingToken.IsRevoked)
            {
                existingToken.IsRevoked = true;
                existingToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
