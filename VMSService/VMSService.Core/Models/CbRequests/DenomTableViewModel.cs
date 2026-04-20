using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Entities;

namespace VMSService.Core.Models.CbRequests
{
    public class DenomTableViewModel
    {
        public IEnumerable<Denomination> Notes { get; init; } = [];
        public IEnumerable<Denomination> Coins { get; init; } = [];
        public bool IsLumpSum { get; set; } = false;
        public decimal LumpSumAmount { get; set; }
        public bool IsEdit { get; set; }
        public IEnumerable<WithdrawalRequestDenomination> SelectedDenominations { get; init; } = [];

    }
}
