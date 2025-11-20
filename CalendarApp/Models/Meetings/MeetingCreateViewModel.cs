using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Models.Meetings
{
    public class MeetingCreateViewModel
    {
        [Required(ErrorMessage = "Моля, изберете начален час.")]
        [Display(Name = "Начален час")]
        public DateTime? StartTime { get; set; }

        [StringLength(100, ErrorMessage = "Мястото трябва да е до 100 символа.")]
        [Display(Name = "Място")]
        public string? Location { get; set; }

        [StringLength(500, ErrorMessage = "Описанието трябва да е до 500 символа.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Моля, изберете категория.")]
        [Display(Name = "Категория")]
        public Guid? CategoryId { get; set; }

        public List<CategoryOptionViewModel> Categories { get; set; } = new();

        public List<MeetingParticipantFormModel> Participants { get; set; } = new();
    }
}
