namespace GameServerManagerWebApp.Models
{
    public class SetupArma3Mod
    {
        public string Name { get; internal set; }
        public string Id { get; internal set; }
        public string Href { get; internal set; }
        public bool IsOK { get; internal set; }
        public string Message { get; internal set; }
        public long Size { get; internal set; }
    }
}
