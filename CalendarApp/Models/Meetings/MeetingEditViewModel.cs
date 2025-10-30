using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Models.Meetings
{
    public class MeetingEditViewModel
    {
        [Required]
        public Guid Id { get; set; }

        public Guid CreatedById { get; set; }

        [Required]
        [Display(Name = "Start time")]
        public DateTime? StartTime { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(500)]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Category")]
        public Guid? CategoryId { get; set; }

        public List<CategoryOptionViewModel> Categories { get; set; } = new();

        public List<MeetingParticipantFormModel> Participants { get; set; } = new();
    }
}
