using System;
using System.ComponentModel.DataAnnotations;

namespace CalendarApp.Data.Models
{
    public class MessageSeen
    {
        [Required]
        public Guid MessageId { get; set; }
        public Message Message { get; set; } = null!;

        [Required]
        public Guid ContactId { get; set; }
        public Contact Contact { get; set; } = null!;

        public DateTime SeenAt { get; set; }
    }
}
