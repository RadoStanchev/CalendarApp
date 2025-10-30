using AutoMapper;
using CalendarApp.Models.Meetings;
using CalendarApp.Services.Categories.Models;
using CalendarApp.Services.Meetings.Models;

namespace CalendarApp.Infrastructure.Mapping
{
    public class MeetingProfile : Profile
    {
        public MeetingProfile()
        {
            CreateMap<MeetingDetailsDto, MeetingDetailsViewModel>();
            CreateMap<MeetingParticipantDto, MeetingParticipantDisplayViewModel>();

            CreateMap<MeetingSummaryDto, MeetingListItemViewModel>();

            CreateMap<MeetingEditDto, MeetingEditViewModel>();
            CreateMap<MeetingParticipantDto, MeetingParticipantFormModel>();

            CreateMap<MeetingCreateViewModel, MeetingCreateDto>()
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime!.Value));

            CreateMap<MeetingEditViewModel, MeetingUpdateDto>()
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime!.Value));

            CreateMap<MeetingParticipantFormModel, MeetingParticipantUpdateDto>();

            CreateMap<ContactSuggestionDto, ContactSuggestionViewModel>();

            CreateMap<CategorySummaryDto, CategoryOptionViewModel>();
        }
    }
}
