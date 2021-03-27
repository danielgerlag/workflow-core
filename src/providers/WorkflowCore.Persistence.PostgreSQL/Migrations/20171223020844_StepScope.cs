using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace WorkflowCore.Persistence.PostgreSQL.Migrations
{
    public partial class StepScope : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scope",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: false,
                defaultValue: 0);        
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                schema: "wfc",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "wfc",
                table: "ExecutionPointer");
            
        }
    }
}
