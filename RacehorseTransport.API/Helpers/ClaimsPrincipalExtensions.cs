using System.Security.Claims;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.API.Helpers
{
    // Dùng trong controller: int userId = User.GetUserId(); hoặc var currentUser = User.ToCurrentUser();
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(JwtClaimNames.UserId);
            if (!int.TryParse(value, out int userId))
            {
                throw new UnauthorizedException("Access token does not contain a valid user id.");
            }
            return userId;
        }

        public static string GetRole(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(JwtClaimNames.Role) ?? string.Empty;
        }

        public static CurrentUser ToCurrentUser(this ClaimsPrincipal user)
        {
            return new CurrentUser
            {
                UserId = user.GetUserId(),
                Role = user.GetRole()
            };
        }
    }
}
