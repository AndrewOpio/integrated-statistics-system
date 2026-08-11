namespace IdentityService.Core.Static
{
    public class SystemPermissions
    {
        public static class USM_ApplicationManagement
        {
            public const string ViewApplications = "USM ApplicationsController Index";
            public const string ViewDetails      = "USM ApplicationsController Details";
            public const string CreateApplication = "USM ApplicationsController Create";
            public const string EditApplication  = "USM ApplicationsController Edit";
            public const string DeleteApplication = "USM ApplicationsController Delete";
        }

        public static class USM_RoleManagement
        {
            public const string ViewRoles  = "USM RolesController Index";
            public const string CreateRole = "USM RolesController Create";
        }

        public static class USM_UserManagement
        {
            public const string ViewUsers      = "USM UsersController Index";
            public const string CreateUser     = "USM UsersController Create";
            public const string UpdateUser     = "USM UsersController Update";
            public const string ManageUserRoles = "USM UsersController ManageRoles";
        }

        // ── CAS (VMSService) Permissions ────────────────────────────

        public static class CAS_EndOfDayDiscrepancies
        {
            public const string View    = "CAS EndOfDayDiscrepanciesController Index";
            public const string Details = "CAS EndOfDayDiscrepanciesController Details";
            public const string Create  = "CAS EndOfDayDiscrepanciesController Create";
            public const string Edit    = "CAS EndOfDayDiscrepanciesController Edit";
        }

        public static class CAS_CbDepositRequests
        {
            public const string View    = "CAS CbDepositRequestsController Index";
            public const string Details = "CAS CbDepositRequestsController Details";
            public const string Create  = "CAS CbDepositRequestsController Create";
            public const string Edit    = "CAS CbDepositRequestsController Edit";
        }
        public static class CAS_CbWithdrawRequests
        {
            public const string View = "CAS CbWithdrawRequestsController Index";
            public const string Details = "CAS CbWithdrawRequestsController Details";
            public const string Create = "CAS CbWithdrawRequestsController Create";
            public const string Edit = "CAS CbWithdrawRequestsController Edit";
        }
        public static class CAS_DepositRequests
        {
            public const string View    = "CAS DepositRequestsController Index";
            public const string Details = "CAS DepositRequestsController Details";
            public const string Create  = "CAS DepositRequestsController Create";
        }

        public static class CAS_DestructionRequests
        {
            public const string View    = "CAS DestructionRequestsController Index";
            public const string Details = "CAS DestructionRequestsController Details";
            public const string Create  = "CAS DestructionRequestsController Create";
            public const string Edit    = "CAS DestructionRequestsController Edit";
        }

        public static class CAS_AgentDetails
        {
            public const string View    = "CAS AgentDetailsController Index";
            public const string Details = "CAS AgentDetailsController Details";
            public const string Create  = "CAS AgentDetailsController Create";
            public const string Edit    = "CAS AgentDetailsController Edit";
        }

        public static class CAS_WithdrawalRequests
        {
            public const string View    = "CAS WithdrawalRequestsController Index";
            public const string Details = "CAS WithdrawalRequestsController Details";
            public const string Create  = "CAS WithdrawalRequestsController Create";
        }

        public static class CAS_ExPrintMints
        {
            public const string View    = "CAS ExPrintMintsController Index";
            public const string Details = "CAS ExPrintMintsController Details";
            public const string Create  = "CAS ExPrintMintsController Create";
            public const string Edit    = "CAS ExPrintMintsController Edit";
        }

        public static class CAS_ReceivedStocks
        {
            public const string View    = "CAS ReceivedStocksController Index";
            public const string Details = "CAS ReceivedStocksController Details";
            public const string Create  = "CAS ReceivedStocksController Create";
        }

        public static class CAS_StockMovement
        {
            public const string View    = "CAS StockMovementController Index";
            public const string Details = "CAS StockMovementController Details";
        }

        public static class CAS_Logistics
        {
            public const string View    = "CAS LogisticsController Index";
            public const string Details = "CAS LogisticsController Details";
            public const string Create  = "CAS LogisticsController Create";
        }

        public static class CAS_Denominations
        {
            public const string View   = "CAS DenominationsController Index";
            public const string Create = "CAS DenominationsController Create";
            public const string Edit   = "CAS DenominationsController Edit";
        }

        public static class CAS_CommercialBankUsers
        {
            public const string View   = "CAS CommercialBankUsersController Index";
            public const string Create = "CAS CommercialBankUsersController Create";
            public const string Edit   = "CAS CommercialBankUsersController Edit";
        }

        public static class CAS_MonthlySurpluses
        {
            public const string View          = "CAS MonthlySurplusesController Index";
            public const string Details       = "CAS MonthlySurplusesController Details";
            public const string Create        = "CAS MonthlySurplusesController Create";
            public const string Edit          = "CAS MonthlySurplusesController Edit";
            public const string CheckPosition = "CAS MonthlySurplusesController CheckPosition";
        }

        public static class CAS_MonthlyShortages
        {
            public const string View     = "CAS MonthlyShortagesController Index";
            public const string Details  = "CAS MonthlyShortagesController Details";
            public const string Generate = "CAS MonthlyShortagesController Generate";
        }

        public static class CAS_MonthlyShortageReimbursements
        {
            public const string View   = "CAS MonthlyShortageReimbursementsController Index";
            public const string Create = "CAS MonthlyShortageReimbursementsController Create";
        }

        public static class CAS_MonthlyCommercialBankSettlements
        {
            public const string View     = "CAS MonthlyCommercialBankSettlementsController Index";
            public const string Details  = "CAS MonthlyCommercialBankSettlementsController Details";
            public const string Generate = "CAS MonthlyCommercialBankSettlementsController Generate";
        }

        public static class CAS_BankingShortages
        {
            public const string View     = "CAS BankingShortagesController Index";
            public const string Details  = "CAS BankingShortagesController Details";
            public const string Generate = "CAS BankingShortagesController Generate";
        }

        public static class CAS_BankingSurpluses
        {
            public const string View    = "CAS BankingSurplusesController Index";
            public const string Details = "CAS BankingSurplusesController Details";
            public const string Create  = "CAS BankingSurplusesController Create";
        }

        public static class CAS_StockTakes
        {
            public const string View         = "CAS StockTakesController Index";
            public const string Details      = "CAS StockTakesController Details";
            public const string Create       = "CAS StockTakesController Create";
            public const string Edit         = "CAS StockTakesController Edit";
            public const string ViewVariance = "CAS StockTakesController ViewVariance";
        }
    }
}
