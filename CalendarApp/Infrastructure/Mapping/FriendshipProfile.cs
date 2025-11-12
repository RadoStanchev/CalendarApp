using AutoMapper;
using CalendarApp.Infrastructure.Formatting;
using CalendarApp.Models.Friendships;
using CalendarApp.Services.Friendships.Models;

namespace CalendarApp.Infrastructure.Mapping
{
    public class FriendshipProfile : Profile
    {
        public FriendshipProfile()
        {
            CreateMap<FriendInfo, FriendViewModel>()
                .ForMember(dest => dest.FriendshipId, opt => opt.MapFrom(src => src.FriendshipId))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => NameFormatter.Format(src.FirstName, src.LastName)));

            CreateMap<FriendRequestInfo, FriendRequestViewModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.TargetUserId))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => NameFormatter.Format(src.TargetFirstName, src.TargetLastName)))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.TargetEmail))
                .ForMember(dest => dest.RequestedOn, opt => opt.MapFrom(src => src.CreatedAt));

            CreateMap<FriendSuggestionInfo, FriendSuggestionViewModel>()
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => NameFormatter.Format(src.FirstName, src.LastName)));
        }
    }
}
