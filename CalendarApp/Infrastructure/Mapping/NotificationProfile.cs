namespace CalendarApp.Infrastructure.Mapping
{
    using AutoMapper;
    using CalendarApp.Data.Models;
    using CalendarApp.Models.Notifications;
    using CalendarApp.Services.Notifications.Models;

    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationDto, NotificationListItemViewModel>();
            CreateMap<NotificationDto, NotificationPreviewViewModel>();
        }
    }
}
