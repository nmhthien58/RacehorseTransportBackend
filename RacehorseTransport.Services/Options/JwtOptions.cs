namespace RacehorseTransport.Services.Options
{
    // Đọc từ section "Jwt" trong appsettings
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty; // tối thiểu 32 ký tự (HS256)
        public int AccessTokenMinutes { get; set; } = 60;
        public int RefreshTokenDays { get; set; } = 7;
    }

    // Tên claim trong access token (FE cũng đọc các claim này)
    public static class JwtClaimNames
    {
        public const string UserId = "sub";
        public const string Email = "email";
        public const string FullName = "name";
        public const string Role = "role";
    }
}
