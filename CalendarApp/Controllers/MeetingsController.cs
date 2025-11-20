using AutoMapper;
using CalendarApp.Data.Models;
using CalendarApp.Infrastructure.Extentions;
using CalendarApp.Models.Meetings;
using CalendarApp.Services.Categories;
using CalendarApp.Services.Meetings;
using CalendarApp.Services.Meetings.Models;
using CalendarApp.Infrastructure.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public async Task<IActionResult> My(string? search)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var trimmedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            var meetings = await meetingService.GetMeetingsForUserAsync(userId, trimmedSearch);

            var model = new MeetingListViewModel
            {
                SearchTerm = trimmedSearch,
                UpcomingMeetings = mapper.Map<List<MeetingListItemViewModel>>(meetings.UpcomingMeetings),
                PastMeetings = mapper.Map<List<MeetingListItemViewModel>>(meetings.PastMeetings)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new MeetingCreateViewModel
            {
                StartTime = BulgarianTime.LocalNow.AddHours(1)
            };

            model.Categories = await GetCategoryOptionsAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MeetingCreateViewModel model)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);

            if (!ModelState.IsValid)
            {
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }

            var category = await categoryService.GetByIdAsync(model.CategoryId!.Value);
            if (category == null)
            {
                ModelState.AddModelError(nameof(model.CategoryId), "Избраната категория не е налична.");
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }

            var dto = mapper.Map<MeetingCreateDto>(model);
            dto.CreatedById = userId;

            try
            {
                var meetingId = await meetingService.CreateMeetingAsync(dto);
                TempData["MeetingMessage"] = "Срещата беше създадена успешно.";
                return RedirectToAction(nameof(Details), new { id = meetingId });
            }
            catch (ArgumentException ex) when (string.Equals(ex.ParamName, nameof(dto.CategoryId), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.CategoryId), "Избраната категория не е налична.");
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
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

            var userId = await userManager.GetUserIdGuidAsync(User);

            if (!ModelState.IsValid)
            {
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }

            var category = await categoryService.GetByIdAsync(model.CategoryId!.Value);
            if (category == null)
            {
                ModelState.AddModelError(nameof(model.CategoryId), "Избраната категория не е налична.");
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
                ModelState.AddModelError(nameof(model.CategoryId), "Избраната категория не е налична.");
                await PopulateParticipantDetailsAsync(model.Participants);
                model.Categories = await GetCategoryOptionsAsync();
                return View(model);
            }

            if (!updated)
            {
                return NotFound();
            }

            TempData["MeetingMessage"] = "Срещата беше обновена.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var dto = await meetingService.GetMeetingDetailsAsync(id, userId);
            if (dto == null)
            {
                return NotFound();
            }

            var model = mapper.Map<MeetingDetailsViewModel>(dto);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SearchContacts(string term, IEnumerable<Guid>? excludeIds)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var idsToExclude = excludeIds ?? Enumerable.Empty<Guid>();
            var results = await meetingService.SearchContactsAsync(userId, term ?? string.Empty, idsToExclude);
            return Json(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid id, ParticipantStatus status, string? returnUrl)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var updated = await meetingService.UpdateParticipantStatusAsync(id, userId, status);

            if (!updated)
            {
                TempData["MeetingError"] = "Не успяхме да обновим отговора ви за тази среща.";
            }
            else
            {
                TempData["MeetingMessage"] = "Отговорът ви за срещата беше обновен.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(My));
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

    }
}
