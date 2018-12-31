using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.MySQL.Migrations
{
    public partial class Events : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnpublishedEvent");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscribeAsOf",
                table: "Subscription",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EventData = table.Column<string>(nullable: true),
                    EventId = table.Column<Guid>(nullable: false),
                    EventKey = table.Column<string>(maxLength: 200, nullable: true),
                    EventName = table.Column<string>(maxLength: 200, nullable: true),
                    EventTime = table.Column<DateTime>(nullable: false),
                    IsProcessed = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.PersistenceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventId",
                table: "Event",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventTime",
                table: "Event",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_Event_IsProcessed",
                table: "Event",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventName_EventKey",
                table: "Event",
                columns: new[] { "EventName", "EventKey" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropColumn(
                name: "SubscribeAsOf",
                table: "Subscription");

            migrationBuilder.CreateTable(
                name: "UnpublishedEvent",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EventData = table.Column<string>(nullable: true),
                    EventKey = table.Column<string>(maxLength: 200, nullable: true),
                    EventName = table.Column<string>(maxLength: 200, nullable: true),
                    PublicationId = table.Column<Guid>(nullable: false),
                    StepId = table.Column<int>(nullable: false),
                    WorkflowId = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnpublishedEvent", x => x.PersistenceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnpublishedEvent_PublicationId",
                table: "UnpublishedEvent",
                column: "PublicationId",
                unique: true);
        }
    }
}
