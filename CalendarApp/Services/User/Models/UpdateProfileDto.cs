using System;

namespace CalendarApp.Services.User.Models
{
    public class UpdateProfileDto
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public DateTime? BirthDate { get; set; }

        public string? Address { get; set; }

        public string? Note { get; set; }
    }
}
