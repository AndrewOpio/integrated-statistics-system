using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DepositProcessingFee : AuditableEntity
    {
        public int Id { get; set; }
        public decimal StartValue { get; set; }
        public decimal EndValue { get; set; }
        public decimal MedianValue { get; set; }
        public decimal Rate { get; set; }
        public decimal Charge { get; set; }
        public int DenominationTypeId { get; set; }
        public bool IsDeleted { get; set; } = false;   
    }
}
