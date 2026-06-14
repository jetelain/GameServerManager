using System;
using System.Collections.Generic;
using GameServerManagerWebApp.Services.Arma3Mods;

namespace GameServerManagerWebApp.Models
{
    internal class ApiModset
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public string Href { get; set; }
        public DateTime LastChangeUTC { get; set; }
        public List<ModsetEntry> Mods { get; set; }
    }
}