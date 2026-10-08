using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string passwordHash);
    }

    public interface ITokenService
    {
        AccessTokenModel CreateAccessToken(User user);
        string CreateRefreshToken();
        string HashRefreshToken(string refreshToken);
    }
}
