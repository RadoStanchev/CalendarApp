using System.Diagnostics;
using CalendarApp.Infrastructure.Extentions;
using CalendarApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalendarApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;

            if (!isAuthenticated)
            {
                return RedirectToAction(nameof(AccountController.Login), typeof(AccountController).GetControllerName());
            }

            return RedirectToAction(nameof(MeetingsController.My), typeof(MeetingsController).GetControllerName());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
