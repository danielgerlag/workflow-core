using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.MySQL.Migrations
{
    public partial class InitialDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Subscription",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                        principalTable: "Workflow",
                        principalColumn: "PersistenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionError",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                        principalTable: "ExecutionPointer",
                        principalColumn: "PersistenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtensionAttribute",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                        principalTable: "ExecutionPointer",
                        principalColumn: "PersistenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionError_ExecutionPointerId",
                table: "ExecutionError",
                column: "ExecutionPointerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPointer_WorkflowId",
                table: "ExecutionPointer",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtensionAttribute_ExecutionPointerId",
                table: "ExtensionAttribute",
                column: "ExecutionPointerId");

            migrationBuilder.CreateIndex(
                name: "IX_UnpublishedEvent_PublicationId",
                table: "UnpublishedEvent",
                column: "PublicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_EventKey",
                table: "Subscription",
                column: "EventKey");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_EventName",
                table: "Subscription",
                column: "EventName");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_SubscriptionId",
                table: "Subscription",
                column: "SubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_InstanceId",
                table: "Workflow",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_NextExecution",
                table: "Workflow",
                column: "NextExecution");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionError");

            migrationBuilder.DropTable(
                name: "ExtensionAttribute");

            migrationBuilder.DropTable(
                name: "UnpublishedEvent");

            migrationBuilder.DropTable(
                name: "Subscription");

            migrationBuilder.DropTable(
                name: "ExecutionPointer");

            migrationBuilder.DropTable(
                name: "Workflow");
        }
    }
}
