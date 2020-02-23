using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.MySQL.Migrations
{
    public partial class Activities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecutionPointerId",
                table: "Subscription",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalToken",
                table: "Subscription",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalTokenExpiry",
                table: "Subscription",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalWorkerId",
                table: "Subscription",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionData",
                table: "Subscription",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutionPointerId",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "ExternalToken",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "ExternalTokenExpiry",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "ExternalWorkerId",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "SubscriptionData",
                table: "Subscription");
        }
    }
}
