namespace RacehorseTransport.Services.BusinessModels
{
    public class UserFilterOptions : BusinessQueryOptions
    {
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateStaffModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateProfileModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }
}
