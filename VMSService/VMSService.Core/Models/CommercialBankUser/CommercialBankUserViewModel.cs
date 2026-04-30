using ConfigurationsService.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Models.CommercialBankUser
{
    public class CommercialBankUserViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CommercialBankId { get; set; }
        public int? CommercialBankBranchId { get; set; }

        // Display properties
        public string? UserName { get; set; }
        public string? UserFullName { get; set; }
        public CommercialBankDto? CommercialBank { get; set; }

        // Dropdown lists
        public IEnumerable<SelectListItem>? CommercialBankList { get; set; }
        public IEnumerable<NonStaffUserViewModel>? NonStaffUserList { get; set; }
    }
}
