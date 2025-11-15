using AutoMapper;
using System;
using System.Linq;
using CalendarApp.Data.Models;
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

            CreateMap<Meeting, MeetingSummaryDto>()
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => $"{src.CreatedBy.FirstName} {src.CreatedBy.LastName}"))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.CategoryColor, opt => opt.MapFrom(src => src.Category != null ? src.Category.Color : null))
                .ForMember(dest => dest.ParticipantCount, opt => opt.MapFrom(src => src.Participants.Count))
                .ForMember(dest => dest.ViewerIsCreator, opt => opt.Ignore())
                .ForMember(dest => dest.ViewerStatus, opt => opt.Ignore())
                .AfterMap((src, dest, ctx) =>
                {
                    if (ctx.Items.TryGetValue("ViewerId", out var viewerObj) && viewerObj is Guid viewerId)
                    {
                        var isCreator = src.CreatedById == viewerId;
                        dest.ViewerIsCreator = isCreator;
                        dest.ViewerStatus = isCreator
                            ? ParticipantStatus.Accepted
                            : src.Participants.FirstOrDefault(p => p.ContactId == viewerId)?.Status;
                    }
                });

            CreateMap<MeetingSummaryDto, MeetingListItemViewModel>();

            CreateMap<MeetingEditDto, MeetingEditViewModel>();
            CreateMap<MeetingParticipantDto, MeetingParticipantFormModel>();

            CreateMap<MeetingCreateViewModel, MeetingCreateDto>()
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime!.Value))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId!.Value));

            CreateMap<MeetingEditViewModel, MeetingUpdateDto>()
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime!.Value))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId!.Value));

            CreateMap<MeetingParticipantFormModel, MeetingParticipantUpdateDto>();

            CreateMap<CategoryDetailsDto, CategoryOptionViewModel>();
        }
    }
}
