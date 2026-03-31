namespace CalendarApp.Infrastructure.Helpers
{
    public static class ParticipantStatusHelper
    {
        public static readonly Dictionary<int, string> Statuses = new()
        {
            { 0, "Чака отговор" },
            { 1, "Прието" },
            { 2, "Отказано" }
        };
        
        public static string GetLabel(int statusId) => 
            Statuses.TryGetValue(statusId, out var label) ? label : "Чака отговор";

        public static string GetCssClass(int statusId) => statusId switch
        {
            1 => "bg-success",
            2 => "bg-danger",
            _ => "bg-warning text-dark"
        };
    }
}