namespace CalendarApp.Services.Friendships.Models;

public class FriendshipRecord
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public Guid ReceiverId { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
