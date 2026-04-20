using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkflowService.Core.Models.WorkflowTransitions
{
    public class WorkflowTransitionViewModel
    {
        public int Id { get; set; }
        public int EntityTypeId { get; set; }
        public string? EntityTypeName { get; set; }
        public int FromStatusId { get; set; }
        public string? FromStatusName { get; set; }
        public int ToStatusId { get; set; }
        public string? ToStatusName { get; set; }
        public string ActionName { get; set; }
        public int RequiredRole { get; set; }
        public string? RequiredRoleName { get; set; }
        public bool RequiresComments { get; set; }
        public string DisplayLabel { get; set; }
        public string ButtonClass { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// Whether to send an email notification when this transition is performed
        /// </summary>
        public bool SendNotification { get; set; }

        /// <summary>
        /// Comma-separated list of role IDs that should receive the notification
        /// </summary>
        public string? NotificationRoleIds { get; set; }

        /// <summary>
        /// Comma-separated list of email addresses that should receive the notification
        /// </summary>
        public string? NotificationEmails { get; set; }

        /// <summary>
        /// Selected role IDs for notification recipients (used in the form)
        /// </summary>
        public List<int> SelectedNotificationRoleIds { get; set; } = new List<int>();

        /// <summary>
        /// Resolved role names for notification recipients (used in display views)
        /// </summary>
        public List<string> NotificationRoleNames { get; set; } = new List<string>();

        /// <summary>
        /// Parsed list of notification email addresses (used in display views)
        /// </summary>
        public List<string> NotificationEmailList { get; set; } = new List<string>();

        // Dropdown lists
        public List<SelectListItem> EntityTypesList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> FromStatusesList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ToStatusesList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> RequiredRoleList { get; set; } = new List<SelectListItem>();
    }
}