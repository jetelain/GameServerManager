using System.Collections.Generic;

namespace Arma3ServerToolbox.ArmaPersist
{
    public class PersistItemInventory
    {
        public PersistItemInventory()
        {
        }

        private PersistItemInventory(List<object> list)
        {
            Name = (string)list[0];
            Items = PersistItem.Load((List<object>)list[1]);
        }

        public static PersistItemInventory Load(List<object> list)
        {
            if (list.Count > 0)
            {
                return new PersistItemInventory(list);
            }
            return null;
        }

        public string Name { get; set; }
        public List<PersistItem> Items { get; set; }
    }
}