using CalendarApp.Hubs;
using CalendarApp.Infrastructure.Background;
using CalendarApp.Infrastructure.Data;
using CalendarApp.Infrastructure.Extensions;
using CalendarApp.Infrastructure.Mapping;
using CalendarApp.Services.Auth;
using CalendarApp.Services.Categories;
using CalendarApp.Services.Categories.Repositories;
using CalendarApp.Services.Friendships.Repositories;
using CalendarApp.Services.Meetings.Repositories;
using CalendarApp.Services.Messages.Repositories;
using CalendarApp.Services.MessageSeens.Repositories;
using CalendarApp.Services.Notifications.Repositories;
using CalendarApp.Services.Friendships;
using CalendarApp.Services.Meetings;
using CalendarApp.Services.Messages;
using CalendarApp.Services.MessageSeens;
using CalendarApp.Services.Notifications;
using CalendarApp.Services.User;
using CalendarApp.Services.User.Repositories;
using CalendarApp.Services.UserPresence;
using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

var bulgarianCulture = new CultureInfo("bg-BG");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("bg-BG");
    options.SupportedCultures = new List<CultureInfo> { bulgarianCulture };
    options.SupportedUICultures = new List<CultureInfo> { bulgarianCulture };
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<CalendarApp.Data.Models.Contact>, PasswordHasher<CalendarApp.Data.Models.Contact>>();
builder.Services.AddScoped<IAuthenticationService, CookieAuthenticationService>();

builder.Services.AddScoped<IUserRepository, DapperUserRepository>();
builder.Services.AddScoped<IFriendshipRepository, DapperFriendshipRepository>();
builder.Services.AddScoped<IMeetingRepository, DapperMeetingRepository>();
builder.Services.AddScoped<IMessageRepository, DapperMessageRepository>();
builder.Services.AddScoped<INotificationRepository, DapperNotificationRepository>();
builder.Services.AddScoped<ICategoryRepository, DapperCategoryRepository>();
builder.Services.AddScoped<IMessageSeenRepository, DapperMessageSeenRepository>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IMessageSeenService, MessageSeenService>();
builder.Services.AddSingleton<IUserPresenceTracker, UserPresenceTracker>();
builder.Services.AddHostedService<MeetingReminderService>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AccountProfile>();
    cfg.AddProfile<MeetingProfile>();
    cfg.AddProfile<NotificationProfile>();
    cfg.AddProfile<ChatProfile>();
    cfg.AddProfile<FriendshipProfile>();
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

if (builder.Configuration.GetValue<bool>("Azure:SignalR:Enabled"))
{
    builder.Services.AddSignalR().AddAzureSignalR(
        builder.Configuration["Azure:SignalR:ConnectionString"]);
}

var app = builder.Build();

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
