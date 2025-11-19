using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Hubs;
using CalendarApp.Infrastructure.Background;
using CalendarApp.Infrastructure.Extensions;
using CalendarApp.Infrastructure.Mapping;
using CalendarApp.Services.Categories;
using CalendarApp.Services.Friendships;
using CalendarApp.Services.Meetings;
using CalendarApp.Services.Messages;
using CalendarApp.Services.MessageSeens;
using CalendarApp.Services.Notifications;
using CalendarApp.Services.User;
using CalendarApp.Services.UserPresence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Azure.SignalR;

var builder = WebApplication.CreateBuilder(args);

var supportedCultures = new[] { "bg-BG" };
var bulgarianCulture = new CultureInfo("bg-BG");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("bg-BG");
    options.SupportedCultures = new List<CultureInfo> { bulgarianCulture };
    options.SupportedUICultures = new List<CultureInfo> { bulgarianCulture };

    // Optional: allow switching culture from ?culture=bg-BG or cookie
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
});

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<Contact, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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
app.PrepareDatabase();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");


app.Run();
