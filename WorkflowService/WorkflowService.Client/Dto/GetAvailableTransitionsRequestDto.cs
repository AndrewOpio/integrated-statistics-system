namespace WorkflowService.Client.Dto
{
    public class GetAvailableTransitionsRequestDto
    {
        /// <summary>
        /// The application acronym (e.g., "VMS" for Vault Management System)
        /// </summary>
        public string ApplicationAcronym { get; set; }

        /// <summary>
        /// The entity type name (e.g., "Invoice", "PurchaseOrder")
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// The current status ID
        /// </summary>
        public int CurrentStatusId { get; set; }

        /// <summary>
        /// The application role ID to filter transitions
        /// </summary>
        public int AppRole { get; set; }
    }
}
