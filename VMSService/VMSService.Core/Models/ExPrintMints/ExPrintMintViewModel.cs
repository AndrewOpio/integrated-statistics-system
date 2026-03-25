using Microsoft.AspNetCore.Http;
using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.ExPrintMintStatusHistories;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.ExPrintMints
{
    public class ExPrintMintViewModel
    {
        public int Id { get; set; }
        public string ExPrintMintReference { get; set; }
        public int CurrencyVendorId { get; set; }
        public CurrencyVendorViewModel? CurrencyVendor { get; set; }
        public int DenominationId { get; set; }
        public DenominationViewModel? Denomination { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Value { get; set; }
        public string BoxNumberPrefix { get; set; }
        public int? BoxNumberRangeStart { get; set; }
        public int? BoxNumberRangeEnd { get; set; }
        public string? SerialNumberPrefix { get; set; }
        public int? SerialNumberRangeStart { get; set; }
        public int? SerialNumberRangeEnd { get; set; }
        public string? Attachment { get; set; }
        public int ApprovedCurrencyProcurementId { get; set; }
        public ApprovedCurrencyProcurementsViewModel? ApprovedCurrencyProcurement { get; set; }
        public int CurrentStatusId { get; set; }
        public string? CurrentStatusName { get; set; }
        public IFormFile AttachmentFile { get; set; }
        public bool IsDeleted { get; set; }
        public int NumberOfBoxes { get; set; }
        public int BranchId { get; set; }
        public BranchViewModel? Branch { get; set; }
        public ICollection<ExPrintMintStatusHistoryViewModel>? StatusHistory { get; set; } = new List<ExPrintMintStatusHistoryViewModel>();
        public ICollection<WorkflowTransitionDto>? AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
    }
}