using Microsoft.AspNetCore.Mvc;

namespace CalendarApp.Infrastructure.Extentions
{
    public static class ControllerExtensions
    {
        public static string GetControllerName(this Type controllerType)
            => controllerType.Name.Replace(nameof(Controller), string.Empty);
    }
}
