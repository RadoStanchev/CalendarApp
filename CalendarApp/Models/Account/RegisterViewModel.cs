using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Models.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Моля, въведете име."), StringLength(50, ErrorMessage = "Името трябва да е до 50 символа.")]
        [Display(Name = "Име")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Моля, въведете фамилия."), StringLength(50, ErrorMessage = "Фамилията трябва да е до 50 символа.")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Моля, въведете имейл адрес."), EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        [Display(Name = "Имейл")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Моля, въведете парола."), DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Паролата трябва да бъде между 6 и 100 символа.")]
        [Display(Name = "Парола")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Паролите трябва да съвпадат.")]
        [Display(Name = "Потвърдете паролата")]
        public string ConfirmPassword { get; set; }
    }
}
