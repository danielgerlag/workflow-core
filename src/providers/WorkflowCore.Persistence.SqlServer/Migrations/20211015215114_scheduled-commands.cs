using Microsoft.EntityFrameworkCore.Migrations;

namespace WorkflowCore.Persistence.SqlServer.Migrations
{
    public partial class scheduledcommands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersistedScheduledCommand",
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
                    table.PrimaryKey("PK_PersistedScheduledCommand", x => x.PersistenceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersistedScheduledCommand_CommandName_Data",
                table: "PersistedScheduledCommand",
                columns: new[] { "CommandName", "Data" },
                unique: true,
                filter: "[CommandName] IS NOT NULL AND [Data] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PersistedScheduledCommand_ExecuteTime",
                table: "PersistedScheduledCommand",
                column: "ExecuteTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersistedScheduledCommand");
        }
    }
}
