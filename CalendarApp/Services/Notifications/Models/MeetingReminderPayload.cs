using System;

namespace CalendarApp.Services.Notifications.Models
{
    public class MeetingReminderPayload
    {
        public Guid NotificationId { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid MeetingId { get; set; }

        public DateTime MeetingStartTime { get; set; }

        public string? MeetingLocation { get; set; }

        public string? MeetingDescription { get; set; }
    }
}
