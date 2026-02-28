namespace CalendarApp.Services.Messages.Models;

public class MessageRecord
{
    public Guid Id { get; set; }
    public Guid? FriendshipId { get; set; }
    public Guid? MeetingId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
