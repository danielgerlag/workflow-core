using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.MySQL.Migrations
{
    public partial class WorkflowInstance_Add_ExecutionErrorCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExecutionErrorCount",
                table: "Workflow",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutionErrorCount",
                table: "Workflow");
        }
    }
}
