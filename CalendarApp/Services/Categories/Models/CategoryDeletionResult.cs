namespace CalendarApp.Services.Categories.Models
{
    public class CategoryDeletionResult
    {
        public bool Succeeded { get; set; }

        public bool NotFound { get; set; }

        public int MeetingsUpdated { get; set; }

        public string? ErrorMessage { get; set; }

        public static CategoryDeletionResult Success(int meetingsUpdated)
            => new CategoryDeletionResult
            {
                Succeeded = true,
                MeetingsUpdated = meetingsUpdated
            };

        public static CategoryDeletionResult Missing()
            => new CategoryDeletionResult
            {
                NotFound = true,
                ErrorMessage = "Category not found."
            };

        public static CategoryDeletionResult Failure(string? message = null)
            => new CategoryDeletionResult
            {
                ErrorMessage = message
            };
    }
}
