using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtOptions _options;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public AccessTokenModel CreateAccessToken(User user)
        {
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Expires = expiresAt.UtcDateTime,
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtClaimNames.UserId, user.UserId.ToString()),
                    new Claim(JwtClaimNames.Email, user.Email),
                    new Claim(JwtClaimNames.FullName, user.FullName),
                    new Claim(JwtClaimNames.Role, user.Role)
                })
            };

            return new AccessTokenModel
            {
                Token = new JsonWebTokenHandler().CreateToken(descriptor),
                ExpiresAt = expiresAt
            };
        }

        public string CreateRefreshToken()
        {
            return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        }

        // Chỉ lưu bản băm của refresh token, không lưu token gốc.
        public string HashRefreshToken(string refreshToken)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(hash);
        }
    }
}
