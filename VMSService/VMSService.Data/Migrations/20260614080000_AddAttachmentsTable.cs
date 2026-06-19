using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    public partial class AddAttachmentsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id                                = table.Column<long>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    FileName                          = table.Column<string>(nullable: false),
                    FilePath                          = table.Column<string>(nullable: false),
                    FileType                          = table.Column<string>(nullable: false),
                    FileSize                          = table.Column<long>(nullable: false),
                    WorkflowTransitionId              = table.Column<int>(nullable: false),
                    TransitionAttachmentRequirementId = table.Column<int>(nullable: false),
                    EntityId                          = table.Column<string>(nullable: true),
                    EntityType                        = table.Column<string>(nullable: false),
                    IsDeleted                         = table.Column<bool>(nullable: false, defaultValue: false),
                    CreatedDate  = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: false),
                    CreatedBy    = table.Column<string>(nullable: false),
                    ModifiedBy   = table.Column<string>(nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_Attachments", x => x.Id));

            migrationBuilder.AddColumn<string>(
                name: "AttachmentIds",
                table: "DestructionRequestStatusHistory",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Attachments");

            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "DestructionRequestStatusHistory");
        }
    }
}
