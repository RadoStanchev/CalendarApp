using System.ComponentModel.DataAnnotations;
using CalendarApp.Services.Meetings.Models;

namespace CalendarApp.Models.Meetings
{
    public class MeetingParticipantFormModel
    {
        [Required(ErrorMessage = "Моля, изберете участник.")]
        [Display(Name = "Участник")]
        public Guid ContactId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Моля, изберете статус.")]
        [Display(Name = "Статус")]
        public ParticipantStatus Status { get; set; } = ParticipantStatus.Pending;
    }
}
