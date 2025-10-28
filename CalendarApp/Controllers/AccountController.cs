using AutoMapper;
using CalendarApp.Data.Models;
using CalendarApp.Models.Account;
using CalendarApp.Services.User;
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

        // ----------------- REGISTER -----------------
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

        // ----------------- LOGIN -----------------
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

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        // ----------------- LOGOUT -----------------
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // ----------------- PROFILE -----------------
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = Guid.Parse(userManager.GetUserId(User));
            var user = await userService.GetByIdAsync(userId);
            var model = mapper.Map<ProfileViewModel>(user);
            return View(model);
        }
    }
}
