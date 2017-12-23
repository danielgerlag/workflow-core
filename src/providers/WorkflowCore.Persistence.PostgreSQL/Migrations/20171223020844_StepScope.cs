using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace WorkflowCore.Persistence.PostgreSQL.Migrations
{
    public partial class StepScope : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scope",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SuccessorIds",
                schema: "wfc",
                table: "ExecutionPointer",
                nullable: true);         
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                schema: "wfc",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "wfc",
                table: "ExecutionPointer");

            migrationBuilder.DropColumn(
                name: "SuccessorIds",
                schema: "wfc",
                table: "ExecutionPointer");            
        }
    }
}
