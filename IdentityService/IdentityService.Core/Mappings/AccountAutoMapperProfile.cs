using AutoMapper;
using IdentityService.Core.Models;
using IdentityService.Data.Entities;

namespace IdentityService.Mappings
{
    public class AccountAutoMapperProfile : Profile
    {
        public AccountAutoMapperProfile()
        {
            CreateMap<User, UserViewModel>().ReverseMap();
            CreateMap<Application, ApplicationViewModel>().ReverseMap();
            CreateMap<Role, RoleViewModel>().ReverseMap();
            CreateMap<ApplicationRole, ApplicationRoleViewModel>().ReverseMap();
            CreateMap<UserApplicationRole, UserApplicationRoleViewModel>().ReverseMap();
        }
    }
}
