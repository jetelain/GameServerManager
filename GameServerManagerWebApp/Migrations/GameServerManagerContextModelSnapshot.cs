﻿// <auto-generated />
using System;
using GameServerManagerWebApp.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GameServerManagerWebApp.Migrations
{
    [DbContext(typeof(GameServerManagerContext))]
    partial class GameServerManagerContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.4");

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameConfigurationFile", b =>
                {
                    b.Property<int>("GameConfigurationFileID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<int>("GameServerConfigurationID")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastChangeUTC")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.HasKey("GameConfigurationFileID");

                    b.HasIndex("GameServerConfigurationID");

                    b.ToTable("GameConfigurationFile");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameLogEvent", b =>
                {
                    b.Property<int>("GameLogEventID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("Duration")
                        .HasColumnType("TEXT");

                    b.Property<int>("GameServerID")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsAggregated")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsFinished")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SteamId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("GameLogEventID");

                    b.HasIndex("GameServerID");

                    b.ToTable("GameLogEvent");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameLogFile", b =>
                {
                    b.Property<int>("GameLogFileID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Filename")
                        .HasColumnType("TEXT");

                    b.Property<int>("GameServerID")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastSyncUTC")
                        .HasColumnType("TEXT");

                    b.Property<long>("ReadSize")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UnreadData")
                        .HasColumnType("TEXT");

                    b.HasKey("GameLogFileID");

                    b.HasIndex("GameServerID");

                    b.ToTable("GameLogFile");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameServer", b =>
                {
                    b.Property<int>("GameServerID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Address")
                        .HasColumnType("TEXT");

                    b.Property<string>("BasePath")
                        .HasColumnType("TEXT");

                    b.Property<int>("ConnectedPlayers")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("HostServerID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Label")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastPollUtc")
                        .HasColumnType("TEXT");

                    b.Property<short>("Port")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserName")
                        .HasColumnType("TEXT");

                    b.HasKey("GameServerID");

                    b.HasIndex("HostServerID");

                    b.ToTable("GameServer");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameServerConfiguration", b =>
                {
                    b.Property<int>("GameServerConfigurationID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AccessToken")
                        .HasColumnType("TEXT");

                    b.Property<string>("EventHref")
                        .HasColumnType("TEXT");

                    b.Property<string>("EventImage")
                        .HasColumnType("TEXT");

                    b.Property<string>("GamePersistName")
                        .HasColumnType("TEXT");

                    b.Property<int>("GameServerID")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Label")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastChangeUTC")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ModsetID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ServerMission")
                        .HasColumnType("TEXT");

                    b.Property<string>("ServerName")
                        .HasColumnType("TEXT");

                    b.Property<string>("ServerPassword")
                        .HasColumnType("TEXT");

                    b.Property<string>("VoipChannel")
                        .HasColumnType("TEXT");

                    b.Property<string>("VoipPassword")
                        .HasColumnType("TEXT");

                    b.Property<string>("VoipServer")
                        .HasColumnType("TEXT");

                    b.HasKey("GameServerConfigurationID");

                    b.HasIndex("GameServerID");

                    b.HasIndex("ModsetID");

                    b.ToTable("GameServerConfiguration");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameServerSyncedFile", b =>
                {
                    b.Property<int>("GameServerSyncedFileID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<int>("GameServerID")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastChangeUTC")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<string>("SyncUri")
                        .HasColumnType("TEXT");

                    b.HasKey("GameServerSyncedFileID");

                    b.HasIndex("GameServerID");

                    b.ToTable("GameServerSyncedFile");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.HostServer", b =>
                {
                    b.Property<int>("HostServerID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Address")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("SshUserName")
                        .HasColumnType("TEXT");

                    b.HasKey("HostServerID");

                    b.ToTable("HostServer");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.Modset", b =>
                {
                    b.Property<int>("ModsetID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AccessToken")
                        .HasColumnType("TEXT");

                    b.Property<string>("ConfigurationFile")
                        .HasColumnType("TEXT");

                    b.Property<int>("Count")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DefinitionFile")
                        .HasColumnType("TEXT");

                    b.Property<int>("GameType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Label")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("ModsetID");

                    b.ToTable("Modset");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameConfigurationFile", b =>
                {
                    b.HasOne("GameServerManagerWebApp.Entites.GameServerConfiguration", "Configuration")
                        .WithMany("Files")
                        .HasForeignKey("GameServerConfigurationID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Configuration");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameLogEvent", b =>
                {
                    b.HasOne("GameServerManagerWebApp.Entites.GameServer", "Server")
                        .WithMany()
                        .HasForeignKey("GameServerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Server");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameLogFile", b =>
                {
                    b.HasOne("GameServerManagerWebApp.Entites.GameServer", "Server")
                        .WithMany()
                        .HasForeignKey("GameServerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Server");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameServer", b =>
                {
                    b.HasOne("GameServerManagerWebApp.Entites.HostServer", "HostServer")
                        .WithMany()
                        .HasForeignKey("HostServerID");

                    b.Navigation("HostServer");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameServerConfiguration", b =>
                {
                    b.HasOne("GameServerManagerWebApp.Entites.GameServer", "GameServer")
                        .WithMany("Configurations")
                        .HasForeignKey("GameServerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GameServerManagerWebApp.Entites.Modset", "Modset")
                        .WithMany()
                        .HasForeignKey("ModsetID");

                    b.Navigation("GameServer");

                    b.Navigation("Modset");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameServerSyncedFile", b =>
                {
                    b.HasOne("GameServerManagerWebApp.Entites.GameServer", "GameServer")
                        .WithMany("SyncFiles")
                        .HasForeignKey("GameServerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GameServer");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameServer", b =>
                {
                    b.Navigation("Configurations");

                    b.Navigation("SyncFiles");
                });

            modelBuilder.Entity("GameServerManagerWebApp.Entites.GameServerConfiguration", b =>
                {
                    b.Navigation("Files");
                });
#pragma warning restore 612, 618
        }
    }
}
