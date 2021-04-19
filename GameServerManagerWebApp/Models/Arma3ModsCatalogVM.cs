using System.Collections.Generic;

namespace GameServerManagerWebApp.Models
{
    public class Arma3ModsCatalogVM
    {
        public List<ModInfoVM> Mods { get; internal set; }
        public string Server { get; internal set; }
    }
}
