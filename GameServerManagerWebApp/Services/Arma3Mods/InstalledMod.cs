namespace GameServerManagerWebApp.Services.Arma3Mods
{
    public sealed class InstalledMod
    {


        public InstalledMod(string modSteamId, long modSize, string modName)
        {
            this.ModSteamId = modSteamId;
            this.ModSize = modSize;
            this.ModName = modName;
        }

        public string ModSteamId { get; }

        public long ModSize { get; }

        public string ModName { get; }
    }
}