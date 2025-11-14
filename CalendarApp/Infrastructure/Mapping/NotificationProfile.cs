using AutoMapper;
using CalendarApp.Data.Models;
using CalendarApp.Models.Notifications;
using CalendarApp.Services.Notifications.Models;

namespace CalendarApp.Infrastructure.Mapping
{
    public class NotificationProfile : Profile
    {
        internal const string MeetingContextKey = nameof(Meeting);

        public NotificationProfile()
        {
            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationDto, NotificationListItemViewModel>();
            CreateMap<NotificationDto, NotificationPreviewViewModel>();
            CreateMap<NotificationCreateDto, Notification>();
            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationDto, NotificationViewModel>();

            CreateMap<Notification, MeetingReminderPayload>()
                .ForMember(dest => dest.NotificationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.MeetingId, opt => opt.MapFrom((src, dest, destMember, context) => ((Meeting)context.Items[MeetingContextKey]).Id))
                .ForMember(dest => dest.MeetingStartTime, opt => opt.MapFrom((src, dest, destMember, context) => ((Meeting)context.Items[MeetingContextKey]).StartTime))
                .ForMember(dest => dest.MeetingLocation, opt => opt.MapFrom((src, dest, destMember, context) => ((Meeting)context.Items[MeetingContextKey]).Location))
                .ForMember(dest => dest.MeetingDescription, opt => opt.MapFrom((src, dest, destMember, context) => ((Meeting)context.Items[MeetingContextKey]).Description));
        }
    }
}
