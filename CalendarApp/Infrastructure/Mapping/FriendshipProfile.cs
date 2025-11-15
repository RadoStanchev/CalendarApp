using AutoMapper;
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
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.AvatarInitials, opt => opt.MapFrom(src => $"{src.FirstName[0]}{src.LastName[0]}"));

            CreateMap<FriendRequestInfo, FriendRequestViewModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.TargetUserId))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => $"{src.TargetFirstName} {src.TargetLastName}"))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.TargetEmail))
                .ForMember(dest => dest.RequestedOn, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.AvatarInitials, opt => opt.MapFrom(src => $"{src.TargetFirstName[0]}{src.TargetLastName[0]}"));

            CreateMap<FriendSuggestionInfo, FriendSuggestionViewModel>()
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.AvatarInitials, opt => opt.MapFrom(src => $"{src.FirstName[0]}{src.LastName[0]}"));
        }
    }
}
