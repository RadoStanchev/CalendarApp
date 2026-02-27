namespace CalendarApp.Services.MessageSeens.Repositories;

public interface IMessageSeenRepository
{
    Task<IReadOnlyCollection<Guid>> GetUnseenFriendshipMessageIdsAsync(Guid userId, Guid friendshipId);
    Task<IReadOnlyCollection<Guid>> GetUnseenMeetingMessageIdsAsync(Guid userId, Guid meetingId);
    Task InsertManyAsync(Guid userId, IEnumerable<Guid> messageIds, DateTime seenAtUtc);
}
