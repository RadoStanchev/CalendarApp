using System.ComponentModel.DataAnnotations;
using CalendarApp.Data.Models;

namespace CalendarApp.Models.Meetings
{
    public class MeetingParticipantFormModel
    {
        [Required]
        public Guid ContactId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required]
        public ParticipantStatus Status { get; set; } = ParticipantStatus.Pending;
    }
}
