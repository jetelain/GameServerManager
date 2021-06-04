using System.Collections.Generic;

namespace Arma3ServerToolbox.ArmaPersist
{
    public class PersistVehicle
    {
        public PersistVehicle()
        {

        }

        public PersistVehicle(List<object> vehicleData, int id)
        {
            VehicleId = id;
            this.Name = (string)vehicleData[0];
            this.IsAlive = (bool)vehicleData[1];
            this.Position = new PersistPosition((List<object>)vehicleData[2]);
            this.Direction = (float)vehicleData[3];
            this.Items = PersistItem.LoadFromCargo((List<object>)vehicleData[4]);
        }

        public int VehicleId { get; set; }
        public string Name { get; set; }
        public bool IsAlive { get; set; }
        public PersistPosition Position { get; set; }
        public float Direction { get; set; }
        public List<PersistItem> Items { get; set; }
    }
}