using AutoMapper;
using CalendarApp.Data.Models;
using CalendarApp.Models.Meetings;
using CalendarApp.Services.Categories;
using CalendarApp.Services.Meetings;
using CalendarApp.Services.Meetings.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CalendarApp.Controllers
{
    [Authorize]
    public class MeetingsController : Controller
    {
        private readonly IMeetingService meetingService;
        private readonly ICategoryService categoryService;
        private readonly IMapper mapper;
        private readonly UserManager<Contact> userManager;

        public MeetingsController(IMeetingService meetingService, ICategoryService categoryService, IMapper mapper, UserManager<Contact> userManager)
        {
            this.meetingService = meetingService;
            this.categoryService = categoryService;
            this.mapper = mapper;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> My()
        {
            var userId = await GetCurrentUserIdAsync();
            var meetings = await meetingService.GetMeetingsForUserAsync(userId);
            var model = new MeetingListViewModel
            {
                Meetings = mapper.Map<List<MeetingListItemViewModel>>(meetings)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new MeetingCreateViewModel
            {
                StartTime = DateTime.UtcNow.AddHours(1)
            };

            model.Categories = await GetCategoryOptionsAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MeetingCreateViewModel model)
        {
            var userId = await GetCurrentUserIdAsync();

            if (!ModelState.IsValid)
            {
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }

            var category = await categoryService.GetByIdAsync(model.CategoryId!.Value);
            if (category == null)
            {
                ModelState.AddModelError(nameof(model.CategoryId), "The selected category is not available.");
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }

            var dto = mapper.Map<MeetingCreateDto>(model);
            dto.CreatedById = userId;

            try
            {
                var meetingId = await meetingService.CreateMeetingAsync(dto);
                TempData["MeetingMessage"] = "Meeting created successfully.";
                return RedirectToAction(nameof(Details), new { id = meetingId });
            }
            catch (ArgumentException ex) when (string.Equals(ex.ParamName, nameof(dto.CategoryId), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.CategoryId), "The selected category is not available.");
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = await GetCurrentUserIdAsync();
            var dto = await meetingService.GetMeetingForEditAsync(id, userId);
            if (dto == null)
            {
                return NotFound();
            }

            var model = mapper.Map<MeetingEditViewModel>(dto);
            model.Categories = await GetCategoryOptionsAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, MeetingEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var userId = await GetCurrentUserIdAsync();

            if (!ModelState.IsValid)
            {
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }

            var category = await categoryService.GetByIdAsync(model.CategoryId!.Value);
            if (category == null)
            {
                ModelState.AddModelError(nameof(model.CategoryId), "The selected category is not available.");
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }

            var dto = mapper.Map<MeetingUpdateDto>(model);
            dto.UpdatedById = userId;

            bool updated;
            try
            {
                updated = await meetingService.UpdateMeetingAsync(dto);
            }
            catch (ArgumentException ex) when (string.Equals(ex.ParamName, nameof(dto.CategoryId), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.CategoryId), "The selected category is not available.");
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }

            if (!updated)
            {
                return NotFound();
            }

            TempData["MeetingMessage"] = "Meeting updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = await GetCurrentUserIdAsync();
            var dto = await meetingService.GetMeetingDetailsAsync(id, userId);
            if (dto == null)
            {
                return NotFound();
            }

            var model = mapper.Map<MeetingDetailsViewModel>(dto);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SearchContacts(string term, string? exclude)
        {
            var userId = await GetCurrentUserIdAsync();
            var excludeIds = ParseExcludeIds(exclude);
            var results = await meetingService.SearchContactsAsync(userId, term ?? string.Empty, excludeIds);
            var viewModels = mapper.Map<List<ContactSuggestionViewModel>>(results);
            return Json(viewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid id, ParticipantStatus status, string? returnUrl)
        {
            var userId = await GetCurrentUserIdAsync();
            var updated = await meetingService.UpdateParticipantStatusAsync(id, userId, status);

            if (!updated)
            {
                TempData["MeetingError"] = "We couldn't update your response for that meeting.";
            }
            else
            {
                TempData["MeetingMessage"] = "Your meeting response was updated.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(My));
        }

        private async Task<Guid> GetCurrentUserIdAsync()
        {
            var user = await userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User not found.");
            return user.Id;
        }

        private async Task PopulateParticipantDetailsAsync(List<MeetingParticipantFormModel> participants)
        {
            if (participants.Count == 0)
            {
                return;
            }

            var summaries = await meetingService.GetContactsAsync(participants.Select(p => p.ContactId));
            var lookup = summaries.ToDictionary(s => s.Id, s => s);

            for (var i = participants.Count - 1; i >= 0; i--)
            {
                var participant = participants[i];
                if (!lookup.TryGetValue(participant.ContactId, out var summary))
                {
                    participants.RemoveAt(i);
                    continue;
                }

                participant.DisplayName = summary.DisplayName;
                participant.Email = summary.Email;
            }
        }

        private async Task<List<CategoryOptionViewModel>> GetCategoryOptionsAsync()
        {
            var categories = await categoryService.GetAllAsync();
            return mapper.Map<List<CategoryOptionViewModel>>(categories);
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
