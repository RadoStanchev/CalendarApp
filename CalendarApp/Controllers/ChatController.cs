using Microsoft.AspNetCore.Mvc;

namespace CalendarApp.Controllers
{
    public class ChatController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
