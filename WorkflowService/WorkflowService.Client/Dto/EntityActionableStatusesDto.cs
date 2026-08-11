namespace WorkflowService.Client.Dto
{
    public class EntityActionableStatusesDto
    {
        public string EntityTypeName { get; set; } = string.Empty;
        public List<int> FromStatusIds { get; set; } = [];
    }
}
