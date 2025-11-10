namespace CalendarApp.Models.Account
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Address { get; set; }
        public string? Note { get; set; }
    }
}
