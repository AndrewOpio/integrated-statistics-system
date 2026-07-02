using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ReceivedStock : AuditableEntity
    {
        public int Id { get; set; }
        public int ExPrintMintId { get; set; }
        public DateTime DateDelivered { get; set; }
        // public int ReceivedStatus { get; set; }
        public string DeliveryAirline { get; set; }
        public decimal TestingFees { get; set; }
        public string TestingFeesCurrency { get; set; }
        public decimal ShippingFees { get; set; }
        public string ShippingFeesCurrency { get; set; }
        public decimal InsuranceFees { get; set; }
        public string InsuranceFeesCurrency { get; set; }
        public decimal HandlingCharges { get; set; }
        public string HandlingChargesCurrency { get; set; }
        public decimal ClearingFees { get; set; }
        public string ClearingFeesCurrency { get; set; }
        public int CurrentStatusId { get; set; }
        public bool IsDeleted { get; set; }
        public bool? IsPartial { get; set; }
        // public ReceivedStatusTypes ReceivedStatusType { get; set; }
        public ExPrintMint ExPrintMint { get; set; }
        public ICollection<ReceivedStockDetails> ReceivedStockDetails { get; set; }
        public string CurrentStatusName { get; set; }
        public int NumberOfBoxes { get; set; }
    }
}
