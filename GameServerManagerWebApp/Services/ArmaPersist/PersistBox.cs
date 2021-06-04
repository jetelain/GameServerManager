using System.Collections.Generic;

namespace Arma3ServerToolbox.ArmaPersist
{
    public class PersistBox
    {
        public PersistBox()
        {

        }

        public PersistBox(List<object> boxData, int id)
        {
            BoxId = id;
            this.Name = (string)boxData[0];
            this.Items = PersistItem.LoadFromCargo((List<object>)boxData[1]);
        }

        public int BoxId { get; set; }
        public string Name { get; set; }
        public List<PersistItem> Items { get; set; }
    }
}