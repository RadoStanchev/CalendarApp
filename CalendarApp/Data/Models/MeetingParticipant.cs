using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Data.Models
{
    public enum ParticipantStatus
    {
        Pending = 0,
        Accepted = 1,
        Declined = 2
    }

    public class MeetingParticipant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid MeetingId { get; set; }
        public Meeting Meeting { get; set; }

        [Required]
        public Guid ContactId { get; set; }
        public Contact Contact { get; set; }

        [Required]
        public ParticipantStatus Status { get; set; } = ParticipantStatus.Pending;
    }
}
