using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Services.Stocks
{
    public interface IStockService
    {
        // DenominationType methods
        Task<ICollection<DenominationType>> GetAllDenominationTypes();
        Task<DenominationType> GetDenominationTypeById(int id);
        Task<bool> CreateDenominationType(DenominationType denominationType);
        Task<bool> UpdateDenominationType(DenominationType denominationType);

        // Denomination methods
        Task<ICollection<Denomination>> GetAllDenominations();
        Task<Denomination> GetDenominationById(int id);
        Task<bool> CreateDenomination(Denomination denomination);
        Task<bool> UpdateDenomination(Denomination denomination);

        // CounterPartyType methods
        Task<ICollection<CounterPartyType>> GetAllCounterPartyTypes();
        Task<CounterPartyType> GetCounterPartyTypeById(int id);
        Task<bool> CreateCounterPartyType(CounterPartyType counterPartyType);
        Task<bool> UpdateCounterPartyType(CounterPartyType counterPartyType);

        // Branch methods
        //Task<ICollection<Branch>> GetAllBranches();
        //Task<Branch> GetBranchById(int id);
        //Task<bool> CreateBranch(Branch branch);
        //Task<bool> UpdateBranch(Branch branch);

        // Category methods
        Task<ICollection<Category>> GetAllCategories();
        Task<Category> GetCategoryById(int id);
        Task<bool> CreateCategory(Category category);
        Task<bool> UpdateCategory(Category category);

        // BranchCategory methods
        Task<ICollection<BranchCategory>> GetAllBranchCategories();
        Task<BranchCategory> GetBranchCategoryById(int id);
        Task<bool> CreateBranchCategory(BranchCategory branchCategory);
        Task<bool> UpdateBranchCategory(BranchCategory branchCategory);

        // CommercialBank methods
        //Task<ICollection<CommercialBank>> GetAllCommercialBanks();
        //Task<CommercialBank> GetCommercialBankById(int id);
        //Task<bool> CreateCommercialBank(CommercialBank commercialBank);
        //Task<bool> UpdateCommercialBank(CommercialBank commercialBank);

        // StockLedger methods
        Task<ICollection<StockLedger>> GetAllStockLedgers();
        Task<ICollection<StockLedger>> GetStockLedgersByStoreId(int storeId);
        Task<StockLedger> GetStockLedgerById(int id);
        Task<ICollection<StockLedgerDetailReportItem>> GetStockLedgerDetailsByBranch(int? branchId = null);
        Task<bool> CreateStockLedger(StockLedger stockLedger);

        // StockLedgerDetail methods
        Task<ICollection<StockLedgerDetail>> GetAllStockLedgerDetails();
        Task<StockLedgerDetail> GetStockLedgerDetailById(int id);
        Task<bool> CreateStockLedgerDetail(StockLedgerDetail stockLedgerDetail);
        Task<bool> UpdateStockLedgerDetail(StockLedgerDetail stockLedgerDetail);

        // SubCategory methods
        Task<ICollection<SubCategory>> GetAllSubCategories();
        Task<SubCategory> GetSubCategoryById(int id);
        Task<bool> CreateSubCategory(SubCategory subCategory);
        Task<bool> UpdateSubCategory(SubCategory subCategory);
    }
}