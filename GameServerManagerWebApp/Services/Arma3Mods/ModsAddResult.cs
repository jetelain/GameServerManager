using System.Collections.Generic;

namespace GameServerManagerWebApp.Services.Arma3Mods
{
    public class ModsAddResult
    {
        private readonly List<long> added;
        private readonly List<long> existing;

        public ModsAddResult(List<long> added, List<long> existing)
        {
            this.added = added;
            this.existing = existing;
        }

        public List<long> Added => added;
        public List<long> Existing => existing;
    }
}
