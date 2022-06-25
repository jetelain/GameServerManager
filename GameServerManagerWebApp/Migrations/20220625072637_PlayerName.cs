using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameServerManagerWebApp.Migrations
{
    public partial class PlayerName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlayerName",
                table: "GameLogEvent",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GamePersistSnapshot",
                columns: table => new
                {
                    GamePersistSnapshotID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameServerID = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GamePersistName = table.Column<string>(type: "TEXT", nullable: true),
                    Backup = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePersistSnapshot", x => x.GamePersistSnapshotID);
                    table.ForeignKey(
                        name: "FK_GamePersistSnapshot_GameServer_GameServerID",
                        column: x => x.GameServerID,
                        principalTable: "GameServer",
                        principalColumn: "GameServerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GamePersistSnapshot_GameServerID",
                table: "GamePersistSnapshot",
                column: "GameServerID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GamePersistSnapshot");

            migrationBuilder.DropColumn(
                name: "PlayerName",
                table: "GameLogEvent");
        }
    }
}
