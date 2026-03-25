using ConfigurationsService.Dto;
using Microsoft.AspNetCore.Mvc.Rendering;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Helpers
{
    public static class CommercialBanksHelper
    {
        public static IEnumerable<SelectListItem> GetCommercialBanksAsSelectList(List<CommercialBankDto> commercialBanks)
        {
            var selectList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select Commercial Bank --" }
            };

            selectList.AddRange(commercialBanks
                .OrderBy(cb => cb.Name)
                .Select(cb => new SelectListItem
                {
                    Value = cb.Id.ToString(),
                    Text = cb.Name
                }));

            return selectList;
        }
    }
}