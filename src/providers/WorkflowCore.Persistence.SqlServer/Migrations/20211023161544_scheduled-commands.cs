using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.SqlServer.Migrations
{
    public partial class scheduledcommands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledCommand",
                schema: "wfc",
                columns: table => new
                {
                    PersistenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Data = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExecuteTime = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledCommand", x => x.PersistenceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledCommand_CommandName_Data",
                schema: "wfc",
                table: "ScheduledCommand",
                columns: new[] { "CommandName", "Data" },
                unique: true,
                filter: "[CommandName] IS NOT NULL AND [Data] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledCommand_ExecuteTime",
                schema: "wfc",
                table: "ScheduledCommand",
                column: "ExecuteTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledCommand",
                schema: "wfc");
        }
    }
}
