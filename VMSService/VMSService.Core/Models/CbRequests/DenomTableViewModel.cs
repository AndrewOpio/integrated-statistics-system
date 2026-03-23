using StockService.Client.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbRequests
{
    public class DenomTableViewModel
    {
        public IEnumerable<DenominationDto> Notes { get; init; } = [];
        public IEnumerable<DenominationDto> Coins { get; init; } = [];
    }
}
