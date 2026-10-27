namespace RacehorseTransport.API.Helpers
{
    public static class HttpContextExtensions
    {
        // IP của người gọi. Railway chạy sau proxy nên ưu tiên địa chỉ đầu tiên trong X-Forwarded-For.
        public static string GetClientIp(this HttpContext context)
        {
            string? forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                return forwarded.Split(',')[0].Trim();
            }
            var ip = context.Connection.RemoteIpAddress;
            if (ip == null)
            {
                return "127.0.0.1";
            }
            return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4().ToString() : ip.ToString() == "::1" ? "127.0.0.1" : ip.ToString();
        }

        public static string? GetUserAgent(this HttpContext context) =>
            context.Request.Headers.UserAgent.FirstOrDefault();
    }
}
