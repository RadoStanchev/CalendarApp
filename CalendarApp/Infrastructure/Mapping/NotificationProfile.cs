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
            CreateMap<(Notification Notification, Meeting Meeting), MeetingReminderNotificationPayload>()
                .ForMember(dest => dest.NotificationId, opt => opt.MapFrom(src => src.Notification.Id))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Notification.Message))
                .ForMember(dest => dest.MeetingId, opt => opt.MapFrom(src => src.Meeting.Id))
                .ForMember(dest => dest.MeetingStartTime, opt => opt.MapFrom(src => src.Meeting.StartTime))
                .ForMember(dest => dest.MeetingLocation, opt => opt.MapFrom(src => src.Meeting.Location))
                .ForMember(dest => dest.MeetingDescription, opt => opt.MapFrom(src => src.Meeting.Description));
        }
    }
}
