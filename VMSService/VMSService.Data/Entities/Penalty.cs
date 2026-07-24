using System;
using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class Penalty : AuditableEntity
    {
        public int Id { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        /// <summary>One of PenaltyConstants.AppliesTo.*</summary>
        [MaxLength(60)]
        public string AppliesTo { get; set; } = null!;

        /// <summary>Null = flat penalty. A label like "per torn package" = per-unit.</summary>
        [MaxLength(60)]
        public string? Unit { get; set; }

        public TimeOnly? CutOffTime { get; set; }

        public decimal? PenaltyValue { get; set; }

        public decimal? PenaltyPercent { get; set; }

        public decimal? PercentCutOff { get; set; }

        public decimal? PenaltyMultiplier { get; set; }

        public bool Active { get; set; } = true;
    }
}
