using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.SqlServer.Migrations
{
    public partial class CorrelationId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "wfc",
                table: "Workflow",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_CorrelationId",
                schema: "wfc",
                table: "Workflow",
                column: "CorrelationId",
                unique: true,
                filter: "[CorrelationId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workflow_CorrelationId",
                schema: "wfc",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "wfc",
                table: "Workflow");
        }
    }
}
