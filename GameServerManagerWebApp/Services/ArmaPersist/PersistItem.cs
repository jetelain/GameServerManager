using System.Collections.Generic;
using System.Linq;

namespace Arma3ServerToolbox.ArmaPersist
{
    public class PersistItem
    {
        public PersistItem()
        {
        }

        public PersistItem(string name, float count)
        {
            Name = name;
            Count = count;
        }
        public PersistItem(List<object> def)
        {
            var first = def[0] as List<object>;
            if (first != null)
            {
                Name = (string)first[0];
                Count = (float)def[1];
            }
            else
            {
                Name = (string)def[0];
                if (def[1] is bool)
                {
                    Count = (bool)def[1] ? 1 : 0;
                }
                else
                {
                    Count = (float)def[1];
                }
            }
        }

        public float Count { get; set; }

        public string Name { get; set; }

        internal static List<PersistItem> Load(List<object> list)
        {
            return list.Select(item => new PersistItem((List<object>)item)).ToList();
        }

        internal static List<PersistItem> LoadFromCargo(List<object> cargo)
        {
            var data = new List<PersistItem>();
            data.AddRange(LoadFromCargoEntry((List<object>)cargo[0]));
            data.AddRange(LoadFromCargoEntry((List<object>)cargo[1]));
            data.AddRange(LoadFromCargoEntry((List<object>)cargo[2]));
            data.AddRange(LoadFromCargoEntry((List<object>)cargo[3]));
            return data;
        }

        internal static List<PersistItem> LoadFromCargoEntry(List<object> cargoSpec)
        {
            var names = (List<object>)cargoSpec[0];
            var counts = (List<object>)cargoSpec[1];
            return names.Select((name, index) => new PersistItem((string)name, (float)counts[index]))
                .GroupBy(i => i.Name)
                .Select(g => new PersistItem(g.Key, g.Sum(i => i.Count)))
                .ToList();
        }
    }
}