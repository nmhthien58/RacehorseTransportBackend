using RacehorseTransport.API.RequestModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    // UserModel -> UserResponse nằm ở AuthApiMapper (dùng chung)
    public static class UserApiMapper
    {
        public static UserFilterOptions ToBusinessOptions(this UserQueryRequest request)
        {
            return new UserFilterOptions
            {
                Search = request.Search,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size,
                Role = request.Role,
                IsActive = request.IsActive
            };
        }

        public static CreateStaffModel ToBusinessModel(this CreateStaffRequest request)
        {
            return new CreateStaffModel
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password,
                PhoneNumber = request.PhoneNumber,
                Role = request.Role.Trim()
            };
        }

        public static UpdateUserModel ToBusinessModel(this UpdateUserRequest request)
        {
            return new UpdateUserModel
            {
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Role = request.Role.Trim()
            };
        }

        public static UpdateProfileModel ToBusinessModel(this UpdateProfileRequest request)
        {
            return new UpdateProfileModel
            {
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber
            };
        }
    }
}
