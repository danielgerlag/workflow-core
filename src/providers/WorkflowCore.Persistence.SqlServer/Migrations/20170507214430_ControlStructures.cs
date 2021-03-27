using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.SqlServer.Migrations
{
    public partial class ControlStructures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionError_ExecutionPointer_ExecutionPointerId",
                schema: "wfc",
                table: "ExecutionError");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionError_ExecutionPointerId",
                schema: "wfc",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "PathTerminator",
                schema: "wfc",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "wfc",
                table: "ExecutionError");

            migrationBuilder.RenameColumn(
                name: "ConcurrentFork",
                schema: "wfc",
                table: "ExecutionPointer",
                newName: "RetryCount");

            migrationBuilder.AlterColumn<string>(
                name: "StepName",
                schema: "wfc",
                table: "ExecutionPointer",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                schema: "wfc",
                table: "ExecutionPointer",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventKey",
                schema: "wfc",
                table: "ExecutionPointer",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Children",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextItem",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PredecessorId",
                schema: "wfc",
                table: "ExecutionPointer",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExecutionPointerId",
                schema: "wfc",
                table: "ExecutionError",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddColumn<string>(
                name: "WorkflowId",
                schema: "wfc",
                table: "ExecutionError",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Children",
                schema: "wfc",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "ContextItem",
                schema: "wfc",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "PredecessorId",
                schema: "wfc",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                schema: "wfc",
                table: "ExecutionError");

            migrationBuilder.RenameColumn(
                name: "RetryCount",
                schema: "wfc",
                table: "ExecutionPointer",
                newName: "ConcurrentFork");

            migrationBuilder.AlterColumn<string>(
                name: "StepName",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventKey",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PathTerminator",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "ExecutionPointerId",
                schema: "wfc",
                table: "ExecutionError",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                schema: "wfc",
                table: "ExecutionError",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionError_ExecutionPointerId",
                schema: "wfc",
                table: "ExecutionError",
                column: "ExecutionPointerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionError_ExecutionPointer_ExecutionPointerId",
                schema: "wfc",
                table: "ExecutionError",
                column: "ExecutionPointerId",
                principalSchema: "wfc",
                principalTable: "ExecutionPointer",
                principalColumn: "PersistenceId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
