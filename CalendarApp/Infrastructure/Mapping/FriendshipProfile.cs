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
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => FormatName(src.FirstName, src.LastName)))
                .ForMember(dest => dest.AvatarInitials, opt => opt.MapFrom(src => BuildInitials(src.FirstName, src.LastName)));

            CreateMap<FriendRequestInfo, FriendRequestViewModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.TargetUserId))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => FormatName(src.TargetFirstName, src.TargetLastName)))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.TargetEmail))
                .ForMember(dest => dest.RequestedOn, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.AvatarInitials, opt => opt.MapFrom(src => BuildInitials(src.TargetFirstName, src.TargetLastName)));

            CreateMap<FriendSuggestionInfo, FriendSuggestionViewModel>()
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => FormatName(src.FirstName, src.LastName)))
                .ForMember(dest => dest.AvatarInitials, opt => opt.MapFrom(src => BuildInitials(src.FirstName, src.LastName)));
        }

        private static string FormatName(string? firstName, string? lastName)
        {
            var first = firstName?.Trim() ?? string.Empty;
            var last = lastName?.Trim() ?? string.Empty;
            var fullName = $"{first} {last}".Trim();

            return fullName;
        }

        private static string BuildInitials(string? firstName, string? lastName)
        {
            var firstInitial = !string.IsNullOrWhiteSpace(firstName) ? firstName.Trim()[0].ToString() : string.Empty;
            var lastInitial = !string.IsNullOrWhiteSpace(lastName) ? lastName.Trim()[0].ToString() : string.Empty;
            var initials = $"{firstInitial}{lastInitial}";

            return string.IsNullOrEmpty(initials) ? "?" : initials.ToUpperInvariant();
        }
    }
}
