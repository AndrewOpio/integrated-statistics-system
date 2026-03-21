using AutoMapper;
using ConfigurationsService.Core.Models.Branches;
using ConfigurationsService.Core.Models.CommercialBankBranches;
using ConfigurationsService.Core.Models.CommercialBanks;
using ConfigurationsService.Core.Models.Districts;
using ConfigurationsService.Data.Entities;
using ConfigurationsService.Dto;

namespace ConfigurationsService.Core.Mappings
{
    public class ConfigurationsAutoMapperProfile : Profile
    {
        public ConfigurationsAutoMapperProfile()
        {
            CreateMap<Branch, BranchViewModel>().ReverseMap();
            CreateMap<CommercialBank, CommercialBankViewModel>().ReverseMap();
            CreateMap<CommercialBankBranch, CommercialBankBranchViewModel>().ReverseMap();
            CreateMap<District, DistrictViewModel>().ReverseMap();
            CreateMap<Branch, BranchDto>().ReverseMap();
            CreateMap<CommercialBank, CommercialBankDto>().ReverseMap();
            CreateMap<CommercialBankBranch, CommercialBankBranchDto>().ReverseMap();
            CreateMap<District, DistrictDto>().ReverseMap();
        }
    }
}
