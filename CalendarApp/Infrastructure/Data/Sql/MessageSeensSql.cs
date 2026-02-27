namespace CalendarApp.Infrastructure.Data.Sql;

public static class MessageSeensSql
{
    public const string UnseenByFriendship = @"SELECT m.Id
FROM dbo.Messages m
WHERE m.FriendshipId = @friendshipId
  AND m.SenderId <> @userId
  AND NOT EXISTS (
      SELECT 1 FROM dbo.MessageSeens ms
      WHERE ms.MessageId = m.Id AND ms.ContactId = @userId
  )";

    public const string UnseenByMeeting = @"SELECT m.Id
FROM dbo.Messages m
WHERE m.MeetingId = @meetingId
  AND m.SenderId <> @userId
  AND NOT EXISTS (
      SELECT 1 FROM dbo.MessageSeens ms
      WHERE ms.MessageId = m.Id AND ms.ContactId = @userId
  )";

    public const string Insert = @"INSERT INTO dbo.MessageSeens (MessageId, ContactId, SeenAt)
VALUES (@MessageId, @ContactId, @SeenAt)";
}
