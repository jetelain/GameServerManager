using System.Collections.Generic;

namespace Arma3ServerToolbox.ArmaPersist
{
    public class PersistPosition
    {
        public PersistPosition()
        {

        }

        public float X { get; set; }

        public float Y { get; set; }

        public PersistPosition(List<object> list)
        {
            this.X = (float)list[0];
            this.Y = (float)list[1];
        }
    }
}