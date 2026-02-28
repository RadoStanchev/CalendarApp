namespace CalendarApp.Infrastructure.Data.Sql;

public static class MeetingsSql
{
    public const string SelectRecordById = @"SELECT TOP (1) Id, StartTime, Location, Description, CategoryId, CreatedById, ReminderSent
FROM dbo.Meetings
WHERE Id = @meetingId";

    public const string CreateProcedure = "dbo.usp_Meeting_Create";
    public const string UpdateProcedure = "dbo.usp_Meeting_Update";
    public const string ParticipantUpsertProcedure = "dbo.usp_MeetingParticipant_Upsert";
    public const string ParticipantDeleteProcedure = "dbo.usp_MeetingParticipant_Delete";
    public const string UpdateParticipantStatusProcedure = "dbo.usp_MeetingParticipant_UpdateStatus";
}
