using System;
using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Models.Account
{
    public class EditProfileViewModel
    {
        public Guid Id { get; set; }

        [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        [Display(Name = "Имейл")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Моля, въведете име.")]
        [StringLength(50, ErrorMessage = "Името трябва да е до 50 символа.")]
        [Display(Name = "Име")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Моля, въведете фамилия.")]
        [StringLength(50, ErrorMessage = "Фамилията трябва да е до 50 символа.")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата на раждане")]
        public DateTime? BirthDate { get; set; }

        [StringLength(100, ErrorMessage = "Адресът трябва да е до 100 символа.")]
        [Display(Name = "Адрес")]
        public string? Address { get; set; }

        [StringLength(250, ErrorMessage = "Бележката трябва да е до 250 символа.")]
        [Display(Name = "Бележка")]
        public string? Note { get; set; }
    }
}
