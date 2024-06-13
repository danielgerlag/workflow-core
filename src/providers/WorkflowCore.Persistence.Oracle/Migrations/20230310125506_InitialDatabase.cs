using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowCore.Persistence.Oracle.Migrations
{
    public partial class InitialDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    EventId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    EventName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    EventKey = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    EventData = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    EventTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    IsProcessed = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.PersistenceId);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionError",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkflowId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    ExecutionPointerId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    ErrorTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Message = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionError", x => x.PersistenceId);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledCommand",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    CommandName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    Data = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    ExecuteTime = table.Column<long>(type: "NUMBER(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledCommand", x => x.PersistenceId);
                });

            migrationBuilder.CreateTable(
                name: "Subscription",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    SubscriptionId = table.Column<Guid>(type: "RAW(16)", maxLength: 200, nullable: false),
                    WorkflowId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    StepId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ExecutionPointerId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    EventName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    EventKey = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    SubscribeAsOf = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    SubscriptionData = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ExternalToken = table.Column<string>(type: "NVARCHAR2(400)", maxLength: 400, nullable: true),
                    ExternalWorkerId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ExternalTokenExpiry = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscription", x => x.PersistenceId);
                });

            migrationBuilder.CreateTable(
                name: "Workflow",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    InstanceId = table.Column<Guid>(type: "RAW(16)", maxLength: 200, nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    Version = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    Reference = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    NextExecution = table.Column<long>(type: "NUMBER(19)", nullable: true),
                    Data = table.Column<string>(type: "CLOB", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    CompleteTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    Status = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflow", x => x.PersistenceId);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionPointer",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkflowId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    Id = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    StepId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Active = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    SleepUntil = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    PersistenceData = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    StartTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    EndTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    EventName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    EventKey = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    EventPublished = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    EventData = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    StepName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    RetryCount = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Children = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ContextItem = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PredecessorId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Outcome = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Scope = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionPointer", x => x.PersistenceId);
                    table.ForeignKey(
                        name: "FK_ExecutionPointer_Wf_WfId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflow",
                        principalColumn: "PersistenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtensionAttribute",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ExecutionPointerId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    AttributeKey = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    AttributeValue = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtensionAttribute", x => x.PersistenceId);
                    table.ForeignKey(
                        name: "FK_ExtAttr_ExPtr_ExPtrId",
                        column: x => x.ExecutionPointerId,
                        principalTable: "ExecutionPointer",
                        principalColumn: "PersistenceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventId",
                table: "Event",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventName_EventKey",
                table: "Event",
                columns: new[] { "EventName", "EventKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Event_EventTime",
                table: "Event",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_Event_IsProcessed",
                table: "Event",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionPointer_WorkflowId",
                table: "ExecutionPointer",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtensionAttribute_ExecutionPointerId",
                table: "ExtensionAttribute",
                column: "ExecutionPointerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledCommand_CommandName_Data",
                table: "ScheduledCommand",
                columns: new[] { "CommandName", "Data" },
                unique: true,
                filter: "\"CommandName\" IS NOT NULL AND \"Data\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledCommand_ExecuteTime",
                table: "ScheduledCommand",
                column: "ExecuteTime");

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
                name: "Event");

            migrationBuilder.DropTable(
                name: "ExecutionError");

            migrationBuilder.DropTable(
                name: "ExtensionAttribute");

            migrationBuilder.DropTable(
                name: "ScheduledCommand");

            migrationBuilder.DropTable(
                name: "Subscription");

            migrationBuilder.DropTable(
                name: "ExecutionPointer");

            migrationBuilder.DropTable(
                name: "Workflow");
        }
    }
}
