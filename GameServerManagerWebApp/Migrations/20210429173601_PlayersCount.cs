using Microsoft.EntityFrameworkCore.Migrations;

namespace GameServerManagerWebApp.Migrations
{
    public partial class PlayersCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GamePersistName",
                table: "GameServerConfiguration",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConnectedPlayers",
                table: "GameServer",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GamePersistName",
                table: "GameServerConfiguration");

            migrationBuilder.DropColumn(
                name: "ConnectedPlayers",
                table: "GameServer");
        }
    }
}
