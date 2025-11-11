using AutoMapper;
using CalendarApp.Data.Models;
using CalendarApp.Infrastructure.Extentions;
using CalendarApp.Models.Account;
using CalendarApp.Services.User;
using CalendarApp.Services.User.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CalendarApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Contact> userManager;
        private readonly SignInManager<Contact> signInManager;
        private readonly IUserService userService;
        private readonly IMapper mapper;

        public AccountController(
            UserManager<Contact> userManager,
            SignInManager<Contact> signInManager,
            IUserService userService,
            IMapper mapper)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.userService = userService;
            this.mapper = mapper;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = mapper.Map<Contact>(model);
            user.UserName = model.Email;
            user.EmailConfirmed = true;

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, false);
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, false);

            if (result.Succeeded)
                return RedirectToAction(nameof(Profile));

            ModelState.AddModelError("", "Невалиден опит за вход.");
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var user = await userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var model = mapper.Map<ProfileViewModel>(user);
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = await userManager.GetUserIdGuidAsync(User);
            var user = await userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var model = mapper.Map<EditProfileViewModel>(user);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var userId = await userManager.GetUserIdGuidAsync(User);

            if (!ModelState.IsValid)
            {
                var user = await userService.GetByIdAsync(userId);
                model.Email = user?.Email ?? model.Email;
                model.Id = userId;
                return View(model);
            }

            var updateDto = mapper.Map<UpdateProfileDto>(model);
            updateDto.Id = userId;

            var updated = await userService.UpdateProfileAsync(updateDto);

            if (!updated)
            {
                model.Id = userId;
                var user = await userService.GetByIdAsync(userId);
                model.Email = user?.Email ?? model.Email;
                ModelState.AddModelError(string.Empty, "Профилът не може да бъде обновен в момента.");
                return View(model);
            }

            return RedirectToAction(nameof(Profile));
        }
    }
}
