using AutoMapper;
using CalendarApp.Data.Models;
using CalendarApp.Models.Notifications;
using CalendarApp.Services.Notifications.Models;

namespace CalendarApp.Infrastructure.Mapping
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationDto, NotificationListItemViewModel>();
            CreateMap<NotificationDto, NotificationPreviewViewModel>();
            CreateMap<NotificationCreateDto, Notification>();
            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationDto, NotificationViewModel>();
        }
    }
}
