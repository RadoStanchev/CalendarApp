using CalendarApp.Data.Models;
using CalendarApp.Models.Account;
using AutoMapper;

namespace CalendarApp.Infrastructure.Mapping
{
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            // Account Mappings
            CreateMap<Contact, RegisterViewModel>().ReverseMap();
            CreateMap<Contact, ProfileViewModel>().ReverseMap();
        }
    }
}
