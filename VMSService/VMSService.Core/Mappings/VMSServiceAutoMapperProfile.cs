using AutoMapper;
using VMSService.Core.Models.ApprovedCurrencyDenominations;
using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.ApprovedCurrencyStatusHistories;
using VMSService.Core.Models.BranchCategories;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.Categories;
using VMSService.Core.Models.CommercialBanks;
using VMSService.Core.Models.CommercialBankUser;
using VMSService.Core.Models.CounterPartyTypes;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.DenominationTypes;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ExPrintMintStatusHistories;
using VMSService.Core.Models.ReceivedStocks;
using VMSService.Core.Models.ReceivedStockStatusHistories;
using VMSService.Core.Models.StockLedgers;
using VMSService.Core.Models.VendorTypes;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Entities;
using VMSService.Core.Services.Identity.IdentityEntities;
using VMSService.Core.Models.AgentDetails;
using ConfigurationsService.Dto;
using VMSService.Core.Model.AgentDetailStatusHistories;

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
            CreateMap<ReceivedStock, ReceivedStockViewModel>().ReverseMap();
            CreateMap<ReceivedStockStatusHistory, ReceivedStockStatusHistoryViewModel>().ReverseMap();

            // StockLedgerDetail mappings
            CreateMap<StockLedgerDetail, StockLedgerDetailViewModel>()
                .ReverseMap();

            CreateMap<StockLedger, StockLedgerViewModel>()
                .ReverseMap();

            CreateMap<CounterPartyType, CounterPartyTypeViewModel>()
                .ReverseMap();

            // Category, BranchCategory
            CreateMap<Category, CategoryViewModel>()
                .ReverseMap();

            CreateMap<BranchCategory, BranchCategoryViewModel>()
                .ReverseMap();

            CreateMap<BranchDto, BranchViewModel>()
                .ReverseMap();

            CreateMap<CommercialBankDto, CommercialBankViewModel>().ReverseMap();
            CreateMap<NonStaffUser, NonStaffUserViewModel>().ReverseMap();
            CreateMap<AgentDetail, AgentDetailViewModel>().ReverseMap();
            CreateMap<AgentDetailStatusHistory, AgentDetailStatusHistoryViewModel>().ReverseMap();
        }
    }
}