using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.SqlServer.Migrations
{
    public partial class Activities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecutionPointerId",
                schema: "wfc",
                table: "Subscription",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalToken",
                schema: "wfc",
                table: "Subscription",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalTokenExpiry",
                schema: "wfc",
                table: "Subscription",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalWorkerId",
                schema: "wfc",
                table: "Subscription",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutionPointerId",
                schema: "wfc",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "ExternalToken",
                schema: "wfc",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "ExternalTokenExpiry",
                schema: "wfc",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "ExternalWorkerId",
                schema: "wfc",
                table: "Subscription");
        }
    }
}
