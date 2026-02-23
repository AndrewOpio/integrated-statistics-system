namespace VMSService.Data.Entities
{
    public class ReceivedStock
    {
        public int Id { get; set; }
        public int ExPrintMintId { get; set; }
        public ExPrintMint ExPrintMint { get; set; }
        public DateTime DateDelivered { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Value { get; set; }
        public string BoxNumberPrefix { get; set; }
        public int? BoxNumberRangeStart { get; set; }
        public int? BoxNumberRangeEnd { get; set; }
        public string? SerialNumberPrefix { get; set; }
        public int? SerialNumberRangeStart { get; set; }
        public int? SerialNumberRangeEnd { get; set; }
        public int CurrentStatusId { get; set; }
        public StatusType CurrentStatus { get; set; }
        public bool IsDeleted { get; set; }
    }
}
