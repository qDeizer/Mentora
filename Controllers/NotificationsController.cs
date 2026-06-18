using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PsikologProje_Void.Models;
using PsikologProje_Void.Services;

namespace PsikologProje_Void.Controllers
{
    [Authorize]
    [Route("Notifications")]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;

        public NotificationsController(INotificationService notificationService, UserManager<User> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet("Recent")]
        public async Task<IActionResult> Recent([FromQuery] int limit = 12)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var notifications = await _notificationService.GetRecentAsync(userId, limit);
            var unreadCount = await _notificationService.CountUnreadAsync(userId);

            return Json(new
            {
                unreadCount,
                items = notifications.Select(n => new
                {
                    n.Id,
                    type = n.Type.ToString(),
                    n.Title,
                    n.Message,
                    n.DeepLink,
                    n.IsRead,
                    createdAtUtc = n.CreatedAtUtc
                })
            });
        }

        [HttpPost("Read/{id:int}")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            await _notificationService.MarkAsReadAsync(userId, id);
            return Ok();
        }

        [HttpPost("ReadAll")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("request-write")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok();
        }
    }
}
