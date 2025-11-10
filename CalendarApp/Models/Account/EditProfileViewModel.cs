using System;
using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Models.Account
{
    public class EditProfileViewModel
    {
        public Guid Id { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Birth date")]
        public DateTime? BirthDate { get; set; }

        [StringLength(100)]
        public string? Address { get; set; }

        [StringLength(250)]
        public string? Note { get; set; }
    }
}
