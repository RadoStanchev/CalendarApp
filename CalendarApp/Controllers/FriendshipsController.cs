using CalendarApp.Data.Models;
using CalendarApp.Models.Friendships;
using CalendarApp.Services.Friendships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CalendarApp.Controllers
{
    [Authorize]
    public class FriendshipsController : Controller
    {
        private readonly IFriendshipService friendshipService;
        private readonly UserManager<Contact> userManager;

        public FriendshipsController(IFriendshipService friendshipService, UserManager<Contact> userManager)
        {
            this.friendshipService = friendshipService;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = await GetCurrentUserIdAsync();
            var friends = await friendshipService.GetFriendsAsync(userId);
            var pending = await friendshipService.GetPendingRequestsAsync(userId);
            var suggestions = await friendshipService.GetSuggestionsAsync(userId);

            var model = new FriendshipsDashboardViewModel
            {
                Friends = friends.Select(f => new FriendViewModel
                {
                    Id = f.UserId,
                    DisplayName = FormatName(f.FirstName, f.LastName),
                    Email = f.Email
                }).ToList(),
                IncomingRequests = pending
                    .Where(r => r.IsIncoming)
                    .Select(r => new FriendRequestViewModel
                    {
                        FriendshipId = r.FriendshipId,
                        UserId = r.TargetUserId,
                        DisplayName = FormatName(r.TargetFirstName, r.TargetLastName),
                        Email = r.TargetEmail,
                        RequestedOn = r.CreatedAt,
                        IsIncoming = true
                    }).ToList(),
                SentRequests = pending
                    .Where(r => !r.IsIncoming)
                    .Select(r => new FriendRequestViewModel
                    {
                        FriendshipId = r.FriendshipId,
                        UserId = r.TargetUserId,
                        DisplayName = FormatName(r.TargetFirstName, r.TargetLastName),
                        Email = r.TargetEmail,
                        RequestedOn = r.CreatedAt,
                        IsIncoming = false
                    }).ToList(),
                Suggestions = suggestions.Select(s => new FriendSuggestionViewModel
                {
                    UserId = s.UserId,
                    DisplayName = FormatName(s.FirstName, s.LastName),
                    Email = s.Email,
                    MutualFriendCount = s.MutualFriendCount
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(Guid receiverId)
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await friendshipService.SendFriendRequestAsync(userId, receiverId);
            TempData["FriendshipMessage"] = result ? "Friend request sent." : "Unable to send friend request.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(Guid friendshipId)
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await friendshipService.AcceptFriendRequestAsync(friendshipId, userId);
            TempData["FriendshipMessage"] = result ? "Friend request accepted." : "Unable to accept this request.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(Guid friendshipId)
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await friendshipService.DeclineFriendRequestAsync(friendshipId, userId);
            TempData["FriendshipMessage"] = result ? "Friend request declined." : "Unable to decline this request.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid friendshipId)
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await friendshipService.CancelFriendRequestAsync(friendshipId, userId);
            TempData["FriendshipMessage"] = result ? "Friend request cancelled." : "Unable to cancel this request.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid friendId)
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await friendshipService.RemoveFriendAsync(userId, friendId);
            TempData["FriendshipMessage"] = result ? "Friend removed." : "Unable to remove friend.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<Guid> GetCurrentUserIdAsync()
        {
            var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
            return user.Id;
        }

        private static string FormatName(string firstName, string lastName)
        {
            var parts = new[] { firstName, lastName }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
            return parts.Length > 0 ? string.Join(" ", parts) : "Unknown";
        }
    }
}
