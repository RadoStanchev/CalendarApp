using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Models.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Моля, въведете имейл адрес."), EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        [Display(Name = "Имейл")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Моля, въведете парола."), DataType(DataType.Password)]
        [Display(Name = "Парола")]
        public string Password { get; set; }

        [Display(Name = "Запомни ме")]
        public bool RememberMe { get; set; }
    }
}
