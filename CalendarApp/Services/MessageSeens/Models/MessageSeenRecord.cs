namespace CalendarApp.Services.MessageSeens.Models;

public class MessageSeenRecord
{
    public Guid MessageId { get; set; }
    public Guid ContactId { get; set; }
    public DateTime SeenAt { get; set; }
}
