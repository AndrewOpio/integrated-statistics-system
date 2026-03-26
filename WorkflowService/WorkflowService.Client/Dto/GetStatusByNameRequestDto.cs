namespace WorkflowService.Client.Dto
{
    public class GetStatusByNameRequestDto
    {
        /// <summary>
        /// The application acronym (e.g., "VMS" for Vault Management System)
        /// </summary>
        public string ApplicationAcronym { get; set; }

        /// <summary>
        /// The status type name
        /// </summary>
        public string StatusName { get; set; }

        /// <summary>
        /// The entity type name
        /// </summary>
        public string EntityTypeName { get; set; }
    }
}
