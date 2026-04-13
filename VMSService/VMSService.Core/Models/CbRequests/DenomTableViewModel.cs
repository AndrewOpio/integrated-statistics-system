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
        public IEnumerable<WithdrawalRequestDenomination> SelectedDenominations { get; init; } = [];
    }
}
