namespace CalendarApp.Controllers
{
    using System;
    using System.Collections.Generic;
    using AutoMapper;
    using CalendarApp.Data.Models;
    using CalendarApp.Models.Notifications;
    using CalendarApp.Services.Notifications;
    using CalendarApp.Services.Notifications.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService notificationService;
        private readonly IMapper mapper;
        private readonly UserManager<Contact> userManager;

        public NotificationsController(
            INotificationService notificationService,
            IMapper mapper,
            UserManager<Contact> userManager)
        {
            this.notificationService = notificationService;
            this.mapper = mapper;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(NotificationReadFilter filter = NotificationReadFilter.All)
        {
            var userId = await GetCurrentUserIdAsync();

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
            var userId = await GetCurrentUserIdAsync();
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

            var userId = await GetCurrentUserIdAsync();
            var marked = await notificationService.MarkAsReadAsync(userId, Id.Value);

            if (!marked)
            {
                return NotFound();
            }

            var unreadCount = await notificationService.GetUnreadCountAsync(userId);
            return Ok(new { success = true, unreadCount });
        }

        private async Task<Guid> GetCurrentUserIdAsync()
        {
            var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("Потребителят не е намерен.");
            return user.Id;
        }
    }
}
