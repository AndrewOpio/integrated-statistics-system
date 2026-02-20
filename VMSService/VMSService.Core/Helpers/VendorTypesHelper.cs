using Microsoft.AspNetCore.Mvc.Rendering;
using VMSService.Data.Entities;

namespace VMSService.Core.Helpers
{
    public static class VendorTypesHelper
    {
        public static List<SelectListItem> GetVendorTypesAsSelectList(List<VendorType> vendorTypes)
        {
            var selectList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select Vendor Type --" }
            };

            selectList.AddRange(
                vendorTypes
                    .Where(vt => !vt.IsDeleted)
                    .OrderBy(vt => vt.Name)
                    .Select(vt => new SelectListItem
                    {
                        Value = vt.Id.ToString(),
                        Text = vt.Name
                    })
            );

            return selectList;
        }
    }
}