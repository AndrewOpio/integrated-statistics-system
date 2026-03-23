using AutoMapper;
using ConfigurationsService.Core.Models.Branches;
using ConfigurationsService.Entities;

namespace ConfigurationsService.Core.Mappings
{
    public class ConfigurationsAutoMapperProfile : Profile
    {
        public ConfigurationsAutoMapperProfile()
        {
            CreateMap<Branch, BranchViewModel>().ReverseMap();
        }
    }
}
