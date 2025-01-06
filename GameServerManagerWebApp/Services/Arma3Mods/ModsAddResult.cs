using System.Collections.Generic;

namespace GameServerManagerWebApp.Services.Arma3Mods
{
    public class ModsAddResult
    {
        private readonly List<string> added;
        private readonly List<string> existing;

        public ModsAddResult(List<string> added, List<string> existing)
        {
            this.added = added;
            this.existing = existing;
        }

        public List<string> Added => added;
        public List<string> Existing => existing;
    }
}
