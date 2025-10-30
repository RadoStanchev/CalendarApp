namespace CalendarApp.Services.Meetings.Models
{
    public class MeetingUpdateDto
    {
        public Guid Id { get; set; }

        public DateTime StartTime { get; set; }

        public string? Location { get; set; }

        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }

        public Guid UpdatedById { get; set; }

        public IReadOnlyCollection<MeetingParticipantUpdateDto> Participants { get; set; } = Array.Empty<MeetingParticipantUpdateDto>();
    }
}
