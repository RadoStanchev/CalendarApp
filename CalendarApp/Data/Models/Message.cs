using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Data.Models
{
    public class Message
    {
        [Key]
        [StringLength(36)]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Optional link to a friendship (1:1 chat)
        public Guid? FriendshipId { get; set; }
        public Friendship? Friendship { get; set; }

        // Optional link to a meeting (group chat)
        public Guid? MeetingId { get; set; }
        public Meeting? Meeting { get; set; }

        // Sender (required)
        [Required]
        public Guid SenderId { get; set; }
        public Contact Sender { get; set; } = null!;

        // Message text
        [Required, StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public ICollection<MessageSeen> SeenBy { get; set; } = [];
    }
}
