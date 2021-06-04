using System.Linq;
using Microsoft.EntityFrameworkCore;
using GameServerManagerWebApp.Entites;

namespace GameServerManagerWebApp.Entites
{
    public class GameServerManagerContext : DbContext
    {
        public GameServerManagerContext(DbContextOptions<GameServerManagerContext> options)
            : base(options)
        {
        }

        public DbSet<HostServer> HostServers { get; set; }
        public DbSet<GameServer> GameServers { get; set; }
        public DbSet<GameConfigurationFile> GameConfigurationFiles { get; set; }
        public DbSet<GameServerConfiguration> GameServerConfigurations { get; set; }
        public DbSet<Modset> Modsets { get; set; }
        public DbSet<GameLogEvent> GameLogEvents { get; set; }
        public DbSet<GameLogFile> GameLogFiles { get; set; }
        public DbSet<GameServerSyncedFile> GameServerSyncedFiles { get; set; }
        public DbSet<GamePersistSnapshot> GamePersistSnapshots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HostServer>().ToTable(nameof(HostServer));
            modelBuilder.Entity<GameServer>().ToTable(nameof(GameServer));
            modelBuilder.Entity<GameServerConfiguration>().ToTable(nameof(GameServerConfiguration));
            modelBuilder.Entity<GameConfigurationFile>().ToTable(nameof(GameConfigurationFile));
            modelBuilder.Entity<Modset>().ToTable(nameof(Modset));
            modelBuilder.Entity<GameLogEvent>().ToTable(nameof(GameLogEvent));
            modelBuilder.Entity<GameLogFile>().ToTable(nameof(GameLogFile));
            modelBuilder.Entity<GameServerSyncedFile>().ToTable(nameof(GameServerSyncedFile));
            modelBuilder.Entity<GamePersistSnapshot>().ToTable(nameof(GamePersistSnapshot));
        }

        internal void InitBaseData()
        {
            if (GameServers.Count() == 0)
            {
                var host = HostServers.Add(new HostServer() { SshUserName = "gamemanager", Name = "Griffon", Address = "griffon.tf-bollore.fr" }).Entity;
                GameServers.Add(new GameServer() { Label = "Griffon 01 (arma3-w, 2502)", HostServer = host, Type = GameServerType.Arma3, UserName = "arma3-w", BasePath = "/home/arma3-w/.steam/steamcmd/server", Address = "109.238.12.108", Port = 2502 });
                GameServers.Add(new GameServer() { Label = "Griffon 02 (arma3-wpublic, 2402)", HostServer = host, Type = GameServerType.Arma3, UserName = "arma3-wpublic", BasePath = "/home/arma3-wpublic/.steam/steamcmd/server", Address = "109.238.12.108", Port = 2402 });
                SaveChanges();
            }
            SaveChanges();
        }

    }
}
