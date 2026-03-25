namespace WorkflowService.Client.Dto
{
    public class GetTransitionByKeyRequestDto
    {
        /// <summary>
        /// The entity type name
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// The from status ID
        /// </summary>
        public int FromStatusId { get; set; }

        /// <summary>
        /// The action name
        /// </summary>
        public string ActionName { get; set; }
    }
}
