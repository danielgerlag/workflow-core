using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.PostgreSQL.Migrations
{
    public partial class InitialDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "wfc");

            migrationBuilder.CreateTable(
                name: "UnpublishedEvent",
                schema: "wfc",
                columns: table => new
                {
                    ClusterKey = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGeneratedOnAdd", true),
                    EventData = table.Column<string>(nullable: true),
                    EventKey = table.Column<string>(maxLength: 200, nullable: true),
                    EventName = table.Column<string>(maxLength: 200, nullable: true),
                    PublicationId = table.Column<Guid>(nullable: false),
                    StepId = table.Column<int>(nullable: false),
                    WorkflowId = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnpublishedEvent", x => x.ClusterKey);
                });

            migrationBuilder.CreateTable(
                name: "Subscription",
                schema: "wfc",
                columns: table => new
                {
                    ClusterKey = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGeneratedOnAdd", true),
                    EventKey = table.Column<string>(maxLength: 200, nullable: true),
                    EventName = table.Column<string>(maxLength: 200, nullable: true),
                    StepId = table.Column<int>(nullable: false),
                    SubscriptionId = table.Column<Guid>(maxLength: 200, nullable: false),
                    WorkflowId = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscription", x => x.ClusterKey);
                });

            migrationBuilder.CreateTable(
                name: "Workflow",
                schema: "wfc",
                columns: table => new
                {
                    ClusterKey = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGeneratedOnAdd", true),
                    Data = table.Column<string>(nullable: true),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    ExecutionPointers = table.Column<string>(nullable: true),
                    InstanceId = table.Column<Guid>(maxLength: 200, nullable: false),
                    NextExecution = table.Column<long>(nullable: true),
                    Version = table.Column<int>(nullable: false),
                    WorkflowDefinitionId = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflow", x => x.ClusterKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnpublishedEvent_PublicationId",
                schema: "wfc",
                table: "UnpublishedEvent",
                column: "PublicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_EventKey",
                schema: "wfc",
                table: "Subscription",
                column: "EventKey");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_EventName",
                schema: "wfc",
                table: "Subscription",
                column: "EventName");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_SubscriptionId",
                schema: "wfc",
                table: "Subscription",
                column: "SubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_InstanceId",
                schema: "wfc",
                table: "Workflow",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_NextExecution",
                schema: "wfc",
                table: "Workflow",
                column: "NextExecution");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnpublishedEvent",
                schema: "wfc");

            migrationBuilder.DropTable(
                name: "Subscription",
                schema: "wfc");

            migrationBuilder.DropTable(
                name: "Workflow",
                schema: "wfc");
        }
    }
}
