using ConfigurationsService.Entities.AuditColumns;

namespace ConfigurationsService.Entities
{
    public class CommercialBankBranch : AuditableEntity
    {
        public int Id { get; set; }
        public int CommercialBankId { get; set; }
        public CommercialBank? CommercialBank { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public int DistrictId { get; set; }
        public District? District { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}