using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Produces("application/json")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationReadService _notificationReadService;

        public NotificationsController(INotificationReadService notificationReadService)
        {
            _notificationReadService = notificationReadService;
        }

        /// <summary>
        /// Lấy thông báo của người dùng hiện tại, sắp xếp mới nhất trước.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<NotificationResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] NotificationQueryRequest request)
        {
            int currentUserId = User.GetUserId();
            var pagedResult = await _notificationReadService.GetNotificationsAsync(
                currentUserId, request.ToBusinessOptions());
            var items = pagedResult.Items.Select(n => n.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<NotificationResponse>.SuccessResult(
                items, pagedResult.ToPagination(), "Notifications retrieved successfully"));
        }

        /// <summary>
        /// Lấy số lượng thông báo chưa đọc của người dùng hiện tại.
        /// </summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<UnreadNotificationCountResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount()
        {
            int count = await _notificationReadService.GetUnreadCountAsync(User.GetUserId());
            var response = new UnreadNotificationCountResponse { Count = count };
            return Ok(ApiResponse<UnreadNotificationCountResponse>.SuccessResult(response));
        }

        /// <summary>
        /// Đánh dấu một thông báo của người dùng hiện tại là đã đọc.
        /// </summary>
        [HttpPatch("{id:long}/read")]
        [ProducesResponseType(typeof(ApiResponse<NotificationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var updated = await _notificationReadService.MarkAsReadAsync(id, User.GetUserId());
            return Ok(ApiResponse<NotificationResponse>.SuccessResult(
                updated.ToResponse(), "Notification marked as read successfully"));
        }

        /// <summary>
        /// Đánh dấu toàn bộ thông báo của người dùng hiện tại là đã đọc.
        /// </summary>
        [HttpPatch("read-all")]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationReadService.MarkAllAsReadAsync(User.GetUserId());
            return Ok(ApiResponse<object?>.SuccessResult(null, "All notifications marked as read successfully"));
        }
    }
}
