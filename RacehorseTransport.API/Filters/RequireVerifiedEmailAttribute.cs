using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Filters
{
    // Gắn lên action mà khách hàng phải xác thực email mới dùng được (thêm ngựa, tạo đơn, nộp giấy tờ).
    // Khách chưa xác thực vẫn đăng nhập và xem được dữ liệu; gọi vào các action này thì nhận 403 kèm hướng dẫn.
    // Nhân sự không bị kiểm tra: tài khoản do Admin tạo được coi là đã xác thực.
    public class RequireVerifiedEmailAttribute : TypeFilterAttribute
    {
        public RequireVerifiedEmailAttribute() : base(typeof(VerifiedEmailFilter))
        {
        }
    }

    public class VerifiedEmailFilter : IAsyncAuthorizationFilter
    {
        private readonly IAuthService _authService;

        public VerifiedEmailFilter(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true || !user.IsInRole(UserRoles.Customer))
            {
                return;
            }

            // Đọc từ DB chứ không đọc từ token, để vừa xác thực xong là dùng được ngay
            if (!await _authService.IsEmailVerifiedAsync(user.GetUserId()))
            {
                throw new ForbiddenException("Please verify your email address before using this feature. Check your inbox or request a new verification email.");
            }
        }
    }
}
