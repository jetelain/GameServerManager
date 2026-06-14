using System;

namespace GameServerManagerWebApp.Models
{
    public class ApiConfig
    {
        public int Id { get; internal set; }
        public string Label { get; internal set; }
        public string Href { get; internal set; }
        public string ModsetName { get; internal set; }
        public int? ModsetCount { get; internal set; }
        public string ModsetHref { get; internal set; }
        public int? ModsetId { get; internal set; }
        public string ModsetAccessToken { get; internal set; }
        public string ServerName { get; internal set; }
        public string ServerAddress { get; internal set; }
        public short? ServerPort { get; internal set; }
        public string ServerPassword { get; internal set; }
        public bool IsActive { get; internal set; }
        public string VoipServer { get; internal set; }
        public string VoipChannel { get; internal set; }
        public string VoipPassword { get; internal set; }
        public DateTime LastChangeUTC { get; internal set; }
    }
}