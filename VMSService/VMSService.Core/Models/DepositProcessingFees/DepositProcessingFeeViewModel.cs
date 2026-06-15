using Microsoft.AspNetCore.Mvc.Rendering;

namespace VMSService.Core.Models.DepositProcessingFees
{
    public class DepositProcessingFeeViewModel
    {
        public int Id { get; set; }
        public decimal StartValue { get; set; }
        public decimal EndValue { get; set; }
        public decimal MedianValue { get; set; }
        public decimal Rate { get; set; }
        public decimal Charge { get; set; }
        public int DenominationTypeId { get; set; }

        // Display name
        public string? DenominationTypeName { get; set; }

        // Dropdown list
        public List<SelectListItem> DenominationTypeList { get; set; } = new List<SelectListItem>();
    }
}