namespace CalendarApp.Controllers
{
    using System;
    using System.Collections.Generic;
    using AutoMapper;
    using CalendarApp.Models.Notifications;
    using CalendarApp.Services.Auth;
    using CalendarApp.Services.Notifications;
    using CalendarApp.Services.Notifications.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService notificationService;
        private readonly IMapper mapper;
        private readonly IAuthenticationService authenticationService;

        public NotificationsController(
            INotificationService notificationService,
            IMapper mapper,
            IAuthenticationService authenticationService)
        {
            this.notificationService = notificationService;
            this.mapper = mapper;
            this.authenticationService = authenticationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(NotificationReadFilter filter = NotificationReadFilter.All)
        {
            var userId = GetCurrentUserId();

            await notificationService.MarkAllAsReadAsync(userId);

            var query = new NotificationQuery
            {
                Filter = filter
            };

            var notifications = await notificationService.GetAsync(userId, query);
            var model = new NotificationListViewModel
            {
                Filter = filter,
                UnreadCount = await notificationService.GetUnreadCountAsync(userId),
                Notifications = mapper.Map<IReadOnlyList<NotificationViewModel>>(notifications)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Recent(int count = 3, bool includeRead = true)
        {
            var userId = GetCurrentUserId();
            var notifications = await notificationService.GetRecentAsync(userId, count, includeRead);
            var notificationModels = mapper.Map<IReadOnlyCollection<NotificationViewModel>>(notifications);
            var unreadCount = await notificationService.GetUnreadCountAsync(userId);

            return Json(new
            {
                unreadCount,
                notifications = notificationModels
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead([FromBody] Guid? Id)
        {
            if (Id == null || Id.Value == Guid.Empty)
            {
                return BadRequest();
            }

            var userId = GetCurrentUserId();
            var marked = await notificationService.MarkAsReadAsync(userId, Id.Value);

            if (!marked)
            {
                return NotFound();
            }

            var unreadCount = await notificationService.GetUnreadCountAsync(userId);
            return Ok(new { success = true, unreadCount });
        }

        private Guid GetCurrentUserId() => authenticationService.GetCurrentUserId(User);
    }
}
