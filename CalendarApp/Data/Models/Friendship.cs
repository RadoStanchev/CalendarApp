using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Data.Models
{
    public enum FriendshipStatus
    {
        Pending,
        Accepted,
        Declined,
        Blocked
    }

    public class Friendship
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid RequesterId { get; set; }
        public Contact Requester { get; set; }

        public Guid ReceiverId { get; set; }
        public Contact Receiver { get; set; }

        [Required]
        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        [Required, MaxLength(100)]
        public string PairKey { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
