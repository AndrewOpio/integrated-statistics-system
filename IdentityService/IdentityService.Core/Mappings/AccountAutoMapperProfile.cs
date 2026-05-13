using AutoMapper;
using IdentityService.Core.Models;
using IdentityService.Data.Entities;
using IdentityService.Dto;

namespace IdentityService.Mappings
{
    public class AccountAutoMapperProfile : Profile
    {
        public AccountAutoMapperProfile()
        {
            CreateMap<User, UserViewModel>().ReverseMap();
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<Application, ApplicationViewModel>().ReverseMap();
            CreateMap<Role, RoleViewModel>().ReverseMap();
            CreateMap<ApplicationRole, ApplicationRoleViewModel>().ReverseMap();
            CreateMap<UserApplicationRole, UserApplicationRoleViewModel>().ReverseMap();
            CreateMap<Application, ApplicationDto>().ReverseMap();
            CreateMap<Role, RoleDto>().ReverseMap();
            CreateMap<ApplicationRole, ApplicationRoleDto>().ReverseMap();
            CreateMap<RecipientDto, RecipientViewModel>().ReverseMap();
        }
    }
}
