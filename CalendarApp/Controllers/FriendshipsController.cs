using AutoMapper;
using CalendarApp.Data.Models;
using CalendarApp.Infrastructure.Extentions;
using CalendarApp.Infrastructure.Formatting;
using CalendarApp.Models.Friendships;
using CalendarApp.Services.Friendships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CalendarApp.Controllers
{
    [Authorize]
    public class FriendshipsController : Controller
    {
        private readonly IFriendshipService friendshipService;
        private readonly UserManager<Contact> userManager;
        private readonly IMapper mapper;

        public FriendshipsController(IFriendshipService friendshipService, UserManager<Contact> userManager, IMapper mapper)
        {
            this.friendshipService = friendshipService;
            this.userManager = userManager;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var friends = await friendshipService.GetFriendsAsync(userId);
            var pending = await friendshipService.GetPendingRequestsAsync(userId);
            var suggestions = await friendshipService.GetSuggestionsAsync(userId);

            var model = new FriendshipsDashboardViewModel
            {
                Friends = mapper.Map<List<FriendViewModel>>(friends),
                IncomingRequests = mapper.Map<List<FriendRequestViewModel>>(pending.Where(r => r.IsIncoming)),
                SentRequests = mapper.Map<List<FriendRequestViewModel>>(pending.Where(r => !r.IsIncoming)),
                Suggestions = mapper.Map<List<FriendSuggestionViewModel>>(suggestions)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(Guid receiverId)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var (result, friendshipId) = await friendshipService.SendFriendRequestAsync(userId, receiverId);
            var message = result ? "Поканата за приятелство е изпратена." : "Поканата за приятелство не може да бъде изпратена.";

            if (IsJsonRequest())
            {
                return Json(new { success = result, message, friendshipId });
            }

            TempData["FriendshipMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(Guid friendshipId)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var result = await friendshipService.AcceptFriendRequestAsync(friendshipId, userId);
            var message = result ? "Поканата за приятелство е приета." : "Тази покана не може да бъде приета.";

            if (IsJsonRequest())
            {
                return Json(new { success = result, message });
            }

            TempData["FriendshipMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(Guid friendshipId)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var result = await friendshipService.DeclineFriendRequestAsync(friendshipId, userId);
            var message = result ? "Поканата за приятелство е отказана." : "Тази покана не може да бъде отказана.";

            if (IsJsonRequest())
            {
                return Json(new { success = result, message });
            }

            TempData["FriendshipMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid friendshipId)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var result = await friendshipService.CancelFriendRequestAsync(friendshipId, userId);
            var message = result ? "Поканата за приятелство е отменена." : "Тази покана не може да бъде отменена.";

            if (IsJsonRequest())
            {
                return Json(new { success = result, message });
            }

            TempData["FriendshipMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Search(string term, string? exclude)
        {
            var userId = (await userManager.GetUserAsync(User)).Id;
            var excludeIds = ParseExcludeIds(exclude);
            var results = await friendshipService.SearchAsync(userId, term ?? string.Empty, excludeIds);

            var payload = results.Select(result => new
            {
                id = result.UserId,
                displayName = NameFormatter.Format(result.FirstName, result.LastName),
                email = result.Email,
                status = result.RelationshipStatus.ToString(),
                friendshipId = result.FriendshipId,
                isIncomingRequest = result.IsIncomingRequest
            });

            return Json(payload);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid friendshipId)
        {
            var userId = (await userManager.GetUserAsync(User)).Id;
            var result = await friendshipService.RemoveFriendAsync(friendshipId, userId);
            var message = result ? "Приятелят беше премахнат." : "Приятелят не може да бъде премахнат.";

            if (IsJsonRequest())
            {
                return Json(new { success = result, message });
            }

            TempData["FriendshipMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        private bool IsJsonRequest()
        {
            if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) && requestedWith == "XMLHttpRequest")
            {
                return true;
            }

            if (Request.Headers.TryGetValue("Accept", out var accept) && accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static IEnumerable<Guid> ParseExcludeIds(string? exclude)
        {
            if (string.IsNullOrWhiteSpace(exclude))
            {
                return Array.Empty<Guid>();
            }

            var ids = new List<Guid>();
            var parts = exclude.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (Guid.TryParse(part, out var id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

    }
}
