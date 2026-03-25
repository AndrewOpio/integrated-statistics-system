using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedNotificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationEmails",
                table: "WorkflowTransitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationRoleIds",
                table: "WorkflowTransitions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendNotification",
                table: "WorkflowTransitions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationEmails",
                table: "WorkflowTransitions");

            migrationBuilder.DropColumn(
                name: "NotificationRoleIds",
                table: "WorkflowTransitions");

            migrationBuilder.DropColumn(
                name: "SendNotification",
                table: "WorkflowTransitions");
        }
    }
}
