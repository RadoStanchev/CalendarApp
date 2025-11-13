using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Data.Models
{
    public class Contact : IdentityUser<Guid>
    {
        [Required, StringLength(50)]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        public string LastName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [StringLength(100)]
        public string? Address { get; set; }

        [StringLength(250)]
        public string? Note { get; set; }

        public ICollection<MeetingParticipant> MeetingParticipants { get; set; } = [];
        public ICollection<Meeting> OwnedMeetings { get; set; } = [];
        public ICollection<Message> SentMessages { get; set; } = [];
        public ICollection<MessageSeen> MessageSeens { get; set; } = [];
        public ICollection<Friendship> SentFriendRequests { get; set; } = [];
        public ICollection<Friendship> ReceivedFriendRequests { get; set; } = [];
        public ICollection<Notification> Notifications { get; set; } = [];
    }
}
