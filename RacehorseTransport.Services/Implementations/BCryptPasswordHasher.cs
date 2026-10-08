using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Verify(string password, string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // PasswordHash trong DB không phải chuỗi BCrypt (ví dụ dữ liệu seed cũ 'HASH_PASS_123')
                return false;
            }
        }
    }
}
