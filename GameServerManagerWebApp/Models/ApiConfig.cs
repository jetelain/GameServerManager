namespace GameServerManagerWebApp.Models
{
    public class ApiConfig
    {
        public string Label { get; internal set; }
        public string Href { get; internal set; }
        public string ModsetName { get; internal set; }
        public int? ModsetCount { get; internal set; }
        public string ModsetHref { get; internal set; }
        public string ServerName { get; internal set; }
        public string ServerAddress { get; internal set; }
        public short? ServerPort { get; internal set; }
        public string ServerPassword { get; internal set; }
        public bool IsActive { get; internal set; }
        public int Id { get; internal set; }
    }
}