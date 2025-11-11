using CalendarApp.Data;
using CalendarApp.Data.Models;
using CalendarApp.Infrastructure.Background;
using CalendarApp.Infrastructure.Extensions;
using CalendarApp.Infrastructure.Mapping;
using CalendarApp.Services.Meetings;
using CalendarApp.Services.Notifications;
using CalendarApp.Services.Categories;
using CalendarApp.Services.Friendships;
using CalendarApp.Services.User;
using CalendarApp.Services.Messages;
using CalendarApp.Services.MessageSeens;
using CalendarApp.Services.UserPresence;
using CalendarApp.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
    cfg.AddProfile<FriendshipProfile>();
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

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
