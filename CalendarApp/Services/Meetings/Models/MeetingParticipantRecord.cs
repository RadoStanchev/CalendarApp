namespace CalendarApp.Services.Meetings.Models;

public class MeetingParticipantRecord
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid ContactId { get; set; }
    public int StatusId { get; set; }
}
