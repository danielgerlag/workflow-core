using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.MySQL.Migrations
{
    public partial class ControlStructures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionError_ExecutionPointer_ExecutionPointerId",
                table: "ExecutionError");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionError_ExecutionPointerId",
                table: "ExecutionError");

            migrationBuilder.DropColumn(
                name: "PathTerminator",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ExecutionError");

            migrationBuilder.RenameColumn(
                name: "ConcurrentFork",
                table: "ExecutionPointer",
                newName: "RetryCount").Annotation("Relational:ColumnType", "int");

            migrationBuilder.AlterColumn<string>(
                name: "StepName",
                table: "ExecutionPointer",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                table: "ExecutionPointer",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventKey",
                table: "ExecutionPointer",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Children",
                table: "ExecutionPointer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextItem",
                table: "ExecutionPointer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PredecessorId",
                table: "ExecutionPointer",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExecutionPointerId",
                table: "ExecutionError",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddColumn<string>(
                name: "WorkflowId",
                table: "ExecutionError",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Children",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "ContextItem",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "PredecessorId",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "ExecutionError");

            migrationBuilder.RenameColumn(
                name: "RetryCount",
                table: "ExecutionPointer",
                newName: "ConcurrentFork").Annotation("Relational:ColumnType", "int");

            migrationBuilder.AlterColumn<string>(
                name: "StepName",
                table: "ExecutionPointer",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                table: "ExecutionPointer",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventKey",
                table: "ExecutionPointer",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PathTerminator",
                table: "ExecutionPointer",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "ExecutionPointerId",
                table: "ExecutionError",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "ExecutionError",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionError_ExecutionPointerId",
                table: "ExecutionError",
                column: "ExecutionPointerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionError_ExecutionPointer_ExecutionPointerId",
                table: "ExecutionError",
                column: "ExecutionPointerId",
                principalTable: "ExecutionPointer",
                principalColumn: "PersistenceId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
