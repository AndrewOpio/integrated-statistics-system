using System;
using System.Collections.Generic;

namespace VMSService.Data.Entities
{
    public class StockMovementDenomination
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public int? NumBagsBoxes { get; set; } // or NumBoxes for internal
        public int? NumBundles { get; set; } // only for branch-to-branch
        public int? CategoryId { get; set; }
        public string? Categorization { get; set; } // "buffer" or "operational"
        public string? ContainerType { get; set; } // "Bag" or "Box" - derived from category (New = Box, others = Bag)
        public decimal SubTotalValue { get; set; }
        public string DenominationType { get; set; } = "Sent"; // "Sent" or "Received"
        public ICollection<StockMovementDenominationSerialNumberRange> SerialNumberRanges { get; set; }
        public int StockMovementId { get; set; }
        public StockMovement StockMovement { get; set; }
    }
}
