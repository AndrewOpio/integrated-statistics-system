using AutoMapper;
using VMSService.Core.Models.ApprovedCurrencyDenominations;
using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.ApprovedCurrencyStatusHistories;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.DenominationTypes;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ExPrintMintStatusHistories;
using VMSService.Core.Models.ReceivedStocks;
using VMSService.Core.Models.StatusTypes;
using VMSService.Core.Models.VendorTypes;
using VMSService.Core.Models.WorkflowTransitions;
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
            CreateMap<ApprovedCurrencyProcurement, ApprovedCurrencyProcurementsViewModel>().ReverseMap();
            CreateMap<ApprovedCurrencyDenomination, ApprovedCurrencyDenominationViewModel>().ReverseMap();
            CreateMap<ExPrintMint, ExPrintMintViewModel>().ReverseMap();
            CreateMap<ExPrintMintStatusHistory, ExPrintMintStatusHistoryViewModel>().ReverseMap();
            CreateMap<ApprovedCurrencyStatusHistory, ApprovedCurrencyStatusHistoryViewModel>().ReverseMap();
            CreateMap<StatusType, StatusTypeViewModel>().ReverseMap();
            CreateMap<ReceivedStock, ReceivedStockViewModel>().ReverseMap();
            CreateMap<WorkflowTransition, WorkflowTransitionViewModel>().ReverseMap();
        }
    }
}