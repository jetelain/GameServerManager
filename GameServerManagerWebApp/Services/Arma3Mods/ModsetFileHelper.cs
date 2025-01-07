using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
#nullable enable

namespace GameServerManagerWebApp.Services.Arma3Mods
{
    public class ModsetFileHelper
    {
        public static string? GetName(XDocument doc)
        {
            return doc.Descendants("meta")
                .Where(m => m.Attribute("name")?.Value == "arma:PresetName")
                .Select(m => m.Attribute("content")?.Value)
                .FirstOrDefault();
        }

        public static List<ModsetEntry> GetModsetEntries(XDocument doc)
        {
            var steamPrefix = "http://steamcommunity.com/sharedfiles/filedetails/?id=";
            var mods = new List<ModsetEntry>();
            foreach (var mod in doc.Descendants("tr").Attributes("data-type").Where(a => a.Value == "ModContainer"))
            {
                var name = mod.Parent!.Descendants("td").Attributes("data-type").Where(a => a.Value == "DisplayName").FirstOrDefault()?.Parent?.FirstNode?.ToString();
                var href = mod.Parent.Descendants("a").Attributes("href").FirstOrDefault()?.Value;
                href = href?.Replace("https:", "http:");
                if (!string.IsNullOrEmpty(href) && href.StartsWith(steamPrefix))
                {
                    var modSteamId = href.Substring(steamPrefix.Length);
                    mods.Add(new ModsetEntry() { SteamId = modSteamId, Name = name, Href = href });
                }
                else
                {
                    mods.Add(new ModsetEntry() { Name = name, Href = href });
                }
            }
            return mods;
        }
    }
}
