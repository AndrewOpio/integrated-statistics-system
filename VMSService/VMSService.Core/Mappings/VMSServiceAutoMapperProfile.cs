using AutoMapper;
using ConfigurationsService.Client;
using VMSService.Core.Model.AgentDetailStatusHistories;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.ApprovedCurrencyDenominations;
using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.ApprovedCurrencyStatusHistories;
using VMSService.Core.Models.Bags;
using VMSService.Core.Models.BranchCategories;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.Categories;
using VMSService.Core.Models.CbDepositRequest;
using VMSService.Core.Models.CbDepositRequests;
using VMSService.Core.Models.CommercialBanks;
using VMSService.Core.Models.CommercialBankUser;
using VMSService.Core.Models.CounterPartyTypes;
using VMSService.Core.Models.BPSMachines;
using VMSService.Core.Models.CountingRooms;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.StockProcessingPlans;
using VMSService.Core.Models.ProcessingBoards;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.DenominationTypes;
using VMSService.Core.Models.DepositFees;
using VMSService.Core.Models.DepositProcessingFees;
using VMSService.Core.Models.ExPrintMintDetails;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ExPrintMintStatusHistories;
using VMSService.Core.Models.MinDepositStandards;
using VMSService.Core.Models.ReceivedStocks;
using VMSService.Core.Models.ReceivedStocksDetails;
using VMSService.Core.Models.ReceivedStockStatusHistories;
using VMSService.Core.Models.StockLedgers;
using VMSService.Core.Models.SubCategories;
using VMSService.Core.Models.Vaults;
using VMSService.Core.Models.VaultSections;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.VendorTypes;
using VMSService.Core.Services.Identity.IdentityEntities;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Entities;

namespace VMSService.Core.Mappings
{
    public class VMSServiceAutoMapperProfile : Profile
    {
        public VMSServiceAutoMapperProfile()
        {
            CreateMap<VendorType, VendorTypeViewModel>().ReverseMap();
            CreateMap<CurrencyVendor, CurrencyVendorViewModel>().ReverseMap();
            CreateMap<BPSMachine, BPSMachineViewModel>().ReverseMap();
            CreateMap<CountingRoom, CountingRoomViewModel>().ReverseMap();
            CreateMap<StockProcessingPlan, StockProcessingPlanViewModel>()
                .ForMember(dest => dest.StockMovementReference, opt => opt.MapFrom(src => src.StockMovement != null ? src.StockMovement.ReferenceNumber : null))
                .ReverseMap()
                .ForMember(dest => dest.StockMovement, opt => opt.Ignore());
            CreateMap<ProcessingBoard, ProcessingBoardViewModel>()
                .ForMember(dest => dest.BPSMachineName, opt => opt.MapFrom(src => src.BPSMachine != null ? src.BPSMachine.Name : null))
                .ForMember(dest => dest.CountingRoomName, opt => opt.MapFrom(src => src.CountingRoom != null ? src.CountingRoom.Name : null))
                .ReverseMap()
                .ForMember(dest => dest.BPSMachine, opt => opt.Ignore())
                .ForMember(dest => dest.CountingRoom, opt => opt.Ignore());
            CreateMap<DenominationType, DenominationTypeViewModel>().ReverseMap();
            CreateMap<Denomination, DenominationViewModel>().ReverseMap();
            CreateMap<ApprovedCurrencyProcurement, ApprovedCurrencyProcurementsViewModel>().ReverseMap();
            CreateMap<ApprovedCurrencyDenomination, ApprovedCurrencyDenominationViewModel>().ReverseMap();
            CreateMap<ExPrintMint, ExPrintMintViewModel>().ReverseMap();
            CreateMap<ExPrintMintDetail, ExPrintMintDetailViewModel>().ReverseMap();
            CreateMap<ExPrintMintStatusHistory, ExPrintMintStatusHistoryViewModel>().ReverseMap();
            CreateMap<ApprovedCurrencyStatusHistory, ApprovedCurrencyStatusHistoryViewModel>().ReverseMap();
            CreateMap<ReceivedStock, ReceivedStockViewModel>().ReverseMap();
            CreateMap<ReceivedStockDetails, ReceivedStockDetailViewModel>().ReverseMap();
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
            CreateMap<DepositRequest, DepositRequestDetailsViewModel>().ReverseMap();
            CreateMap<DepositRequestStatusHistory, DepositRequestStatusHistoryViewModel>().ReverseMap();
            CreateMap<WithdrawalRequestStatusHistory, WithdrawalRequestStatusHistoryViewModel>().ReverseMap();
            CreateMap<MinDepositStandard, MinDepositStandardViewModel>().ReverseMap();
            CreateMap<DepositProcessingFee, DepositProcessingFeeViewModel>().ReverseMap();
            CreateMap<DepositFee, DepositFeeViewModel>().ReverseMap();
            CreateMap<SubCategory, SubCategoryViewModel>().ReverseMap();
            CreateMap<Bag, BagViewModel>().ReverseMap();
            CreateMap<BagDenomination, BagDenominationViewModel>().ReverseMap();

            // Vault and VaultSection mappings
            CreateMap<Vault, VaultViewModel>().ReverseMap();
            CreateMap<VaultSection, VaultSectionViewModel>()
                .ForMember(dest => dest.VaultName, opt => opt.MapFrom(src => src.Vault.Name))
                .ReverseMap();
        }
    }
}