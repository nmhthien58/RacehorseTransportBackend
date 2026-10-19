using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.API.Hubs
{
    // Mặc định SignalR lấy user id từ claim NameIdentifier, nhưng token của hệ thống giữ claim ngắn "sub"
    // (MapInboundClaims = false), nên chỉ cho SignalR đọc "sub" để Clients.User(userId) tìm đúng người.
    public class SubClaimUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirstValue(JwtClaimNames.UserId);
        }
    }
}
