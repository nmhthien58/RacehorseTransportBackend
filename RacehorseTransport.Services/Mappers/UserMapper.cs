using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class UserMapper
    {
        // Không bao giờ đưa PasswordHash ra khỏi tầng service
        public static UserModel ToBusinessModel(this User entity)
        {
            return new UserModel
            {
                UserId = entity.UserId,
                FullName = entity.FullName,
                Email = entity.Email,
                PhoneNumber = entity.PhoneNumber,
                Role = entity.Role,
                IsActive = entity.IsActive,
                EmailVerified = entity.EmailVerifiedAt != null,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
