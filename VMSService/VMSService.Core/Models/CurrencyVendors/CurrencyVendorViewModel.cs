using Microsoft.AspNetCore.Mvc.Rendering;
using VMSService.Core.Models.VendorTypes;

namespace VMSService.Core.Models.CurrencyVendors
{
    public class CurrencyVendorViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Country { get; set; }
        public int VendorTypeId { get; set; }
        public VendorTypeViewModel? VendorType { get; set; }
        public List<SelectListItem> CountryList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> VendorTypeList { get; set; } = new List<SelectListItem>();
    }
}