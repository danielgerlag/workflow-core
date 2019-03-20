using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.MySQL.Migrations
{
    public partial class StepScope : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "ExecutionPointer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ExecutionPointer",
                nullable: false,
                defaultValue: 0);        
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ExecutionPointer");
            
        }
    }
}
