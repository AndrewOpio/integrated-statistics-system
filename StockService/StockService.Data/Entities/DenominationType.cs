using StockService.Data.AuditColumns;

namespace StockService.Data.Entities
{
    public class DenominationType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<Denomination> Denominations { get; set; }
    }
}
