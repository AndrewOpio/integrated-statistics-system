using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockTake : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; }
        public int BranchId { get; set; }
        public DateTime CountDate { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; }

        // Set when this record is a correction of an earlier count that didn't tally.
        public int? AmendsStockTakeId { get; set; }
        public StockTake? AmendsStockTake { get; set; }

        // Set true on the original once an amendment referencing it exists, so it becomes
        // read-only — the amendment is the new source of truth for that count.
        public bool IsLocked { get; set; }

        public ICollection<StockTakeDetail>? StockTakeDetails { get; set; }
        public ICollection<StockTakeStatusHistory>? StockTakeStatusHistory { get; set; }
    }
}
