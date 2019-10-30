using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.PostgreSQL.Migrations
{
    public partial class ExecutionErrors_AdditionalExceptionDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HelpLink",
                schema: "wfc",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "wfc",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StackTrace",
                schema: "wfc",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetSiteModule",
                schema: "wfc",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetSiteName",
                schema: "wfc",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "wfc",
                table: "ExecutionError",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelpLink",
                schema: "wfc",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "wfc",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "StackTrace",
                schema: "wfc",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "TargetSiteModule",
                schema: "wfc",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "TargetSiteName",
                schema: "wfc",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "wfc",
                table: "ExecutionError");
        }
    }
}
