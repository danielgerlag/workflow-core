using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.PostgreSQL.Migrations
{
    public partial class WfReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reference",
                schema: "wfc",
                table: "Workflow",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reference",
                schema: "wfc",
                table: "Workflow");
        }
    }
}
