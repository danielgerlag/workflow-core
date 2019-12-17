using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.MySQL.Migrations
{
    public partial class ExecutionErrors_AdditionalExceptionDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HelpLink",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StackTrace",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetSiteModule",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetSiteName",
                table: "ExecutionError",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ExecutionError",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelpLink",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "StackTrace",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "TargetSiteModule",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "TargetSiteName",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ExecutionError");
        }
    }
}
