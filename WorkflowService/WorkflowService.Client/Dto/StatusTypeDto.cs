namespace WorkflowService.Client.Dto
{
    public class StatusTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int EntityTypeId { get; set; }
        public EntityTypeDto EntityType { get; set; }
    }
}
