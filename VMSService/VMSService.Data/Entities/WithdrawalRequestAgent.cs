using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class WithdrawalRequestAgent : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WithdrawalRequestId { get; set; }
        public WithdrawalRequest WithdrawalRequest { get; set; } = null!;

        public int AgentId { get; set; }             // FK to agent table
        public string EnteredBy { get; set; } = string.Empty;  // "Cb" or "BOU"

        // ── BOU only ─────────────────────────────
        // Required when Side = "BOU" — explains why the commercial bank was replaced
        public byte[]? AttachmentData { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentContentType { get; set; } // e.g. "application/pdf", "image/jpeg"
        public string? AttachmentRemarks { get; set; }     // reason for collector change
    }
}

