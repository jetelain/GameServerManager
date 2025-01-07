using GameServerManagerWebApp.Services.Arma3Mods;

namespace GameServerManagerWebApp.Models
{
    public class ModsetModViewModel
    {
        public string Name { get; set; }
        public string SteamId { get; set; }
        public string Href { get; set; }
        public long Size => Installed?.ModSize ?? 0;
        public InstalledMod Installed { get; set; }
    }
}