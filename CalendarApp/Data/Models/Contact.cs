using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Data.Models
{
    public class Contact
    {
        [Key]
        public Guid Id { get; set; }

        [Required, StringLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        [StringLength(1000)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100)]
        public string SecurityStamp { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [StringLength(100)]
        public string? Address { get; set; }

        [StringLength(250)]
        public string? Note { get; set; }

        public ICollection<Friendship> FriendshipsRequested { get; set; } = [];
        public ICollection<Friendship> FriendshipsReceived { get; set; } = [];
        public ICollection<Meeting> MeetingsCreated { get; set; } = [];
        public ICollection<Message> MessagesSent { get; set; } = [];
        public ICollection<MeetingParticipant> Meetings { get; set; } = [];
        public ICollection<Message> Messages { get; set; } = [];
        public ICollection<Friendship> Friendships { get; set; } = [];
        public ICollection<MessageSeen> MessageSeens { get; set; } = [];
        public ICollection<Notification> Notifications { get; set; } = [];
    }
}
