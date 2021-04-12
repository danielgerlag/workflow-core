using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
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

            migrationBuilder.CreateTable(
                name: "Subscription",
                schema: "wfc",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    EventKey = table.Column<string>(maxLength: 200, nullable: true),
                    EventName = table.Column<string>(maxLength: 200, nullable: true),
                    StepId = table.Column<int>(nullable: false),
                    SubscriptionId = table.Column<Guid>(maxLength: 200, nullable: false),
                    WorkflowId = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscription", x => x.PersistenceId);
                });

            migrationBuilder.CreateTable(
                name: "Workflow",
                schema: "wfc",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CompleteTime = table.Column<DateTime>(nullable: true),
                    CreateTime = table.Column<DateTime>(nullable: false),
                    Data = table.Column<string>(nullable: true),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    InstanceId = table.Column<Guid>(maxLength: 200, nullable: false),
                    NextExecution = table.Column<long>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    WorkflowDefinitionId = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflow", x => x.PersistenceId);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionPointer",
                schema: "wfc",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Active = table.Column<bool>(nullable: false),
                    ConcurrentFork = table.Column<int>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: true),
                    EventData = table.Column<string>(nullable: true),
                    EventKey = table.Column<string>(nullable: true),
                    EventName = table.Column<string>(nullable: true),
                    EventPublished = table.Column<bool>(nullable: false),
                    Id = table.Column<string>(maxLength: 50, nullable: true),
                    PathTerminator = table.Column<bool>(nullable: false),
                    PersistenceData = table.Column<string>(nullable: true),
                    SleepUntil = table.Column<DateTime>(nullable: true),
                    StartTime = table.Column<DateTime>(nullable: true),
                    StepId = table.Column<int>(nullable: false),
                    StepName = table.Column<string>(nullable: true),
                    WorkflowId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionPointer", x => x.PersistenceId);
                    table.ForeignKey(
                        name: "FK_ExecutionPointer_Workflow_WorkflowId",
                        column: x => x.WorkflowId,
                        principalSchema: "wfc",
                        principalTable: "Workflow",
                        principalColumn: "PersistenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionError",
                schema: "wfc",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ErrorTime = table.Column<DateTime>(nullable: false),
                    ExecutionPointerId = table.Column<long>(nullable: false),
                    Id = table.Column<string>(maxLength: 50, nullable: true),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionError", x => x.PersistenceId);
                    table.ForeignKey(
                        name: "FK_ExecutionError_ExecutionPointer_ExecutionPointerId",
                        column: x => x.ExecutionPointerId,
                        principalSchema: "wfc",
                        principalTable: "ExecutionPointer",
                        principalColumn: "PersistenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtensionAttribute",
                schema: "wfc",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    AttributeKey = table.Column<string>(maxLength: 100, nullable: true),
                    AttributeValue = table.Column<string>(nullable: true),
                    ExecutionPointerId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtensionAttribute", x => x.PersistenceId);
                    table.ForeignKey(
                        name: "FK_ExtensionAttribute_ExecutionPointer_ExecutionPointerId",
                        column: x => x.ExecutionPointerId,
                        principalSchema: "wfc",
                        principalTable: "ExecutionPointer",
                        principalColumn: "PersistenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionError_ExecutionPointerId",
                schema: "wfc",
                table: "ExecutionError",
                column: "ExecutionPointerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPointer_WorkflowId",
                schema: "wfc",
                table: "ExecutionPointer",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtensionAttribute_ExecutionPointerId",
                schema: "wfc",
                table: "ExtensionAttribute",
                column: "ExecutionPointerId");

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
                name: "ExecutionError",
                schema: "wfc");

            migrationBuilder.DropTable(
                name: "ExtensionAttribute",
                schema: "wfc");

            migrationBuilder.DropTable(
                name: "UnpublishedEvent",
                schema: "wfc");

            migrationBuilder.DropTable(
                name: "Subscription",
                schema: "wfc");

            migrationBuilder.DropTable(
                name: "ExecutionPointer",
                schema: "wfc");

            migrationBuilder.DropTable(
                name: "Workflow",
                schema: "wfc");
        }
    }
}
