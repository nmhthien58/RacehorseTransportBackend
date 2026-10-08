namespace RacehorseTransport.Repositories.Entities
{
    // identity.user_tokens: token dùng một lần, có hạn (refresh token, xác thực email, đặt lại mật khẩu).
    // Chỉ lưu mã băm của token; chuỗi gốc chỉ nằm ở phía người dùng.
    public class UserToken
    {
        public long TokenId { get; set; }
        public int UserId { get; set; }
        public string TokenType { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? UsedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
    }
}
