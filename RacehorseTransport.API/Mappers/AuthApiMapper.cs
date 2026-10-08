using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class AuthApiMapper
    {
        public static RegisterModel ToBusinessModel(this RegisterRequest request)
        {
            return new RegisterModel
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password,
                PhoneNumber = request.PhoneNumber
            };
        }

        public static UserResponse ToResponse(this UserModel model)
        {
            return new UserResponse
            {
                UserId = model.UserId,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Role = model.Role,
                IsActive = model.IsActive,
                CreatedAt = model.CreatedAt
            };
        }

        public static AuthResponse ToResponse(this AuthResultModel model)
        {
            return new AuthResponse
            {
                AccessToken = model.AccessToken,
                AccessTokenExpiresAt = model.AccessTokenExpiresAt,
                RefreshToken = model.RefreshToken,
                User = model.User.ToResponse()
            };
        }
    }
}
