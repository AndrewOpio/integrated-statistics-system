using AutoMapper;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.DenominationTypes;
using VMSService.Core.Models.VendorTypes;
using VMSService.Data.Entities;

namespace VMSService.Core.Mappings
{
    public class VMSServiceAutoMapperProfile : Profile
    {
        public VMSServiceAutoMapperProfile()
        {
            CreateMap<VendorType, VendorTypeViewModel>().ReverseMap();
            CreateMap<CurrencyVendor, CurrencyVendorViewModel>().ReverseMap();
            CreateMap<DenominationType, DenominationTypeViewModel>().ReverseMap();
            CreateMap<Denomination, DenominationViewModel>().ReverseMap();
        }
    }
}