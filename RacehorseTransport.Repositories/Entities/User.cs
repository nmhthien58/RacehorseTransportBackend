using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // identity.users
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = UserRoles.Customer;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // null = chưa xác thực email. Tài khoản nhân sự do Admin tạo được coi là đã xác thực ngay.
        public DateTimeOffset? EmailVerifiedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Horse> Horses { get; set; } = new List<Horse>();
    }
}
