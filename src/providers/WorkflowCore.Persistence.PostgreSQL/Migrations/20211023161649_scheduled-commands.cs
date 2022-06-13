using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace WorkflowCore.Persistence.PostgreSQL.Migrations
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommandName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Data = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                unique: true);

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
