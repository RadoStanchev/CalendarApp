using CalendarApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CalendarApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<Contact, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<MeetingParticipant> MeetingParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageSeen> MessageSeens { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Friendship> Friendships { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Meeting>()
                .HasOne(m => m.CreatedBy)
                .WithMany(c => c.OwnedMeetings)
                .HasForeignKey(m => m.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(c => c.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Friendship)
                .WithMany(f => f.Messages)
                .HasForeignKey(m => m.FriendshipId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<MessageSeen>()
                .HasKey(r => new { r.MessageId, r.ContactId });

            builder.Entity<MessageSeen>()
                .HasOne(r => r.Message)
                .WithMany(m => m.SeenBy)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MessageSeen>()
                .HasOne(r => r.Contact)
                .WithMany(c => c.MessageSeens)
                .HasForeignKey(r => r.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MessageSeen>()
                .HasIndex(r => new { r.MessageId, r.ContactId })
                .IsUnique();

            builder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany(c => c.SentFriendRequests)
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Friendship>()
                .HasOne(f => f.Receiver)
                .WithMany(c => c.ReceivedFriendRequests)
                .HasForeignKey(f => f.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
