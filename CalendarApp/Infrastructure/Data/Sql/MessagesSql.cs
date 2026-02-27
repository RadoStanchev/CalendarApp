namespace CalendarApp.Infrastructure.Data.Sql;

public static class MessagesSql
{
    public const string HasFriendshipAccess = @"SELECT CAST(CASE WHEN EXISTS (
SELECT 1 FROM dbo.Friendships
WHERE Id = @friendshipId
  AND Status = @acceptedStatus
  AND (RequesterId = @userId OR ReceiverId = @userId)) THEN 1 ELSE 0 END AS bit)";

    public const string HasMeetingAccess = @"SELECT CAST(CASE WHEN EXISTS (
SELECT 1 FROM dbo.Meetings m
WHERE m.Id = @meetingId
AND (
    m.CreatedById = @userId OR EXISTS (
        SELECT 1 FROM dbo.MeetingParticipants mp
        WHERE mp.MeetingId = m.Id AND mp.ContactId = @userId AND mp.Status = @acceptedStatus
    )
)) THEN 1 ELSE 0 END AS bit)";

    public const string Sender = "SELECT TOP 1 Id, FirstName, LastName FROM dbo.Contacts WHERE Id = @userId";

    public const string MeetingInfo = "SELECT TOP 1 Id, StartTime, Location FROM dbo.Meetings WHERE Id = @meetingId";

    public const string Insert = @"INSERT INTO dbo.Messages (Id, FriendshipId, MeetingId, SenderId, Content, SentAt)
VALUES (@Id, @FriendshipId, @MeetingId, @SenderId, @Content, @SentAt)";

    public const string SelectRecentByFriendship = @"SELECT TOP (@take)
m.Id, m.SenderId, m.Content, m.SentAt, m.FriendshipId, m.MeetingId,
c.FirstName AS SenderFirstName, c.LastName AS SenderLastName
FROM dbo.Messages m
INNER JOIN dbo.Contacts c ON c.Id = m.SenderId
WHERE m.FriendshipId = @friendshipId
ORDER BY m.SentAt DESC";

    public const string SelectRecentByMeeting = @"SELECT TOP (@take)
m.Id, m.SenderId, m.Content, m.SentAt, m.FriendshipId, m.MeetingId,
c.FirstName AS SenderFirstName, c.LastName AS SenderLastName
FROM dbo.Messages m
INNER JOIN dbo.Contacts c ON c.Id = m.SenderId
WHERE m.MeetingId = @meetingId
ORDER BY m.SentAt DESC";
}
