using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameServerManagerWebApp.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HostServer",
                columns: table => new
                {
                    HostServerID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    SshUserName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostServer", x => x.HostServerID);
                });

            migrationBuilder.CreateTable(
                name: "Modset",
                columns: table => new
                {
                    ModsetID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameType = table.Column<int>(type: "INTEGER", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: true),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    DefinitionFile = table.Column<string>(type: "TEXT", nullable: true),
                    ConfigurationFile = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modset", x => x.ModsetID);
                });

            migrationBuilder.CreateTable(
                name: "GameServer",
                columns: table => new
                {
                    GameServerID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HostServerID = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Port = table.Column<short>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    BasePath = table.Column<string>(type: "TEXT", nullable: true),
                    LastPollUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameServer", x => x.GameServerID);
                    table.ForeignKey(
                        name: "FK_GameServer_HostServer_HostServerID",
                        column: x => x.HostServerID,
                        principalTable: "HostServer",
                        principalColumn: "HostServerID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameLogEvent",
                columns: table => new
                {
                    GameLogEventID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GameServerID = table.Column<int>(type: "INTEGER", nullable: false),
                    SteamId = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    IsFinished = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAggregated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameLogEvent", x => x.GameLogEventID);
                    table.ForeignKey(
                        name: "FK_GameLogEvent_GameServer_GameServerID",
                        column: x => x.GameServerID,
                        principalTable: "GameServer",
                        principalColumn: "GameServerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameLogFile",
                columns: table => new
                {
                    GameLogFileID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameServerID = table.Column<int>(type: "INTEGER", nullable: false),
                    Filename = table.Column<string>(type: "TEXT", nullable: true),
                    LastSyncUTC = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadSize = table.Column<long>(type: "INTEGER", nullable: false),
                    UnreadData = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameLogFile", x => x.GameLogFileID);
                    table.ForeignKey(
                        name: "FK_GameLogFile_GameServer_GameServerID",
                        column: x => x.GameServerID,
                        principalTable: "GameServer",
                        principalColumn: "GameServerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameServerConfiguration",
                columns: table => new
                {
                    GameServerConfigurationID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: true),
                    GameServerID = table.Column<int>(type: "INTEGER", nullable: false),
                    ServerName = table.Column<string>(type: "TEXT", nullable: true),
                    ServerPassword = table.Column<string>(type: "TEXT", nullable: true),
                    ServerMission = table.Column<string>(type: "TEXT", nullable: true),
                    VoipServer = table.Column<string>(type: "TEXT", nullable: true),
                    VoipChannel = table.Column<string>(type: "TEXT", nullable: true),
                    VoipPassword = table.Column<string>(type: "TEXT", nullable: true),
                    EventHref = table.Column<string>(type: "TEXT", nullable: true),
                    EventImage = table.Column<string>(type: "TEXT", nullable: true),
                    ModsetID = table.Column<int>(type: "INTEGER", nullable: true),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    LastChangeUTC = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameServerConfiguration", x => x.GameServerConfigurationID);
                    table.ForeignKey(
                        name: "FK_GameServerConfiguration_GameServer_GameServerID",
                        column: x => x.GameServerID,
                        principalTable: "GameServer",
                        principalColumn: "GameServerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameServerConfiguration_Modset_ModsetID",
                        column: x => x.ModsetID,
                        principalTable: "Modset",
                        principalColumn: "ModsetID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameServerSyncedFile",
                columns: table => new
                {
                    GameServerSyncedFileID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameServerID = table.Column<int>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    SyncUri = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    LastChangeUTC = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameServerSyncedFile", x => x.GameServerSyncedFileID);
                    table.ForeignKey(
                        name: "FK_GameServerSyncedFile_GameServer_GameServerID",
                        column: x => x.GameServerID,
                        principalTable: "GameServer",
                        principalColumn: "GameServerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameConfigurationFile",
                columns: table => new
                {
                    GameConfigurationFileID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameServerConfigurationID = table.Column<int>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    LastChangeUTC = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameConfigurationFile", x => x.GameConfigurationFileID);
                    table.ForeignKey(
                        name: "FK_GameConfigurationFile_GameServerConfiguration_GameServerConfigurationID",
                        column: x => x.GameServerConfigurationID,
                        principalTable: "GameServerConfiguration",
                        principalColumn: "GameServerConfigurationID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameConfigurationFile_GameServerConfigurationID",
                table: "GameConfigurationFile",
                column: "GameServerConfigurationID");

            migrationBuilder.CreateIndex(
                name: "IX_GameLogEvent_GameServerID",
                table: "GameLogEvent",
                column: "GameServerID");

            migrationBuilder.CreateIndex(
                name: "IX_GameLogFile_GameServerID",
                table: "GameLogFile",
                column: "GameServerID");

            migrationBuilder.CreateIndex(
                name: "IX_GameServer_HostServerID",
                table: "GameServer",
                column: "HostServerID");

            migrationBuilder.CreateIndex(
                name: "IX_GameServerConfiguration_GameServerID",
                table: "GameServerConfiguration",
                column: "GameServerID");

            migrationBuilder.CreateIndex(
                name: "IX_GameServerConfiguration_ModsetID",
                table: "GameServerConfiguration",
                column: "ModsetID");

            migrationBuilder.CreateIndex(
                name: "IX_GameServerSyncedFile_GameServerID",
                table: "GameServerSyncedFile",
                column: "GameServerID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameConfigurationFile");

            migrationBuilder.DropTable(
                name: "GameLogEvent");

            migrationBuilder.DropTable(
                name: "GameLogFile");

            migrationBuilder.DropTable(
                name: "GameServerSyncedFile");

            migrationBuilder.DropTable(
                name: "GameServerConfiguration");

            migrationBuilder.DropTable(
                name: "GameServer");

            migrationBuilder.DropTable(
                name: "Modset");

            migrationBuilder.DropTable(
                name: "HostServer");
        }
    }
}
