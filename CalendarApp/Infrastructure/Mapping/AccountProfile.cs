using AutoMapper;
using CalendarApp.Models.Account;
using CalendarApp.Services.User.Models;

namespace CalendarApp.Infrastructure.Mapping
{
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            CreateMap<UserRecord, RegisterViewModel>().ReverseMap();
            CreateMap<UserRecord, ProfileViewModel>().ReverseMap();
            CreateMap<UserRecord, EditProfileViewModel>().ReverseMap();
            CreateMap<EditProfileViewModel, UpdateProfileDto>();
        }
    }
}
