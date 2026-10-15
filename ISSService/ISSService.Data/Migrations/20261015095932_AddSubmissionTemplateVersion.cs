using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionTemplateVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DataCollectionTemplateVersionId",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_DataCollectionTemplateVersionId",
                table: "Submissions",
                column: "DataCollectionTemplateVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_DataCollectionTemplateVersions_DataCollectionTemplateVersionId",
                table: "Submissions",
                column: "DataCollectionTemplateVersionId",
                principalTable: "DataCollectionTemplateVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_DataCollectionTemplateVersions_DataCollectionTemplateVersionId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_DataCollectionTemplateVersionId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "DataCollectionTemplateVersionId",
                table: "Submissions");
        }
    }
}
