using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockTakeDetail : AuditableEntity
    {
        public int Id { get; set; }
        public int StockTakeId { get; set; }
        public StockTake? StockTake { get; set; }
        public int CategoryId { get; set; }
        public int VaultSectionId { get; set; }
        public VaultSection? VaultSection { get; set; }
        public int DenominationId { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Value { get; set; }
        public bool IsDeleted { get; set; }
    }
}
