using System;
using System.Collections.Generic;

namespace VMSService.Data.Entities
{
    public class StockMovementDenominationSerialNumberRange
    {
        public int Id { get; set; }
        public string Prefix { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public int StockMovementDenominationId { get; set; }
        public StockMovementDenomination StockMovementDenomination { get; set; }
    }
}
