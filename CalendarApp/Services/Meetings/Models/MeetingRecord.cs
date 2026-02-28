namespace CalendarApp.Services.Meetings.Models;

public class MeetingRecord
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Guid CreatedById { get; set; }
    public bool ReminderSent { get; set; }
}
