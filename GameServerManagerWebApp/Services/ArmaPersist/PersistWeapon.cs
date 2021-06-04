using System.Collections.Generic;

namespace Arma3ServerToolbox.ArmaPersist
{
    public class PersistWeapon
    {
        public PersistWeapon()
        {

        }

        private PersistWeapon(List<object> list)
        {
            Name = (string)list[0];
            var magInfos = ((List<object>)list[4]);
            if (magInfos !=null && magInfos.Count > 0)
            {
                Mag = (string)magInfos[0];
            }
        }

        public static PersistWeapon Load(List<object> list)
        {
            if (list.Count > 0)
            {
                return new PersistWeapon(list);
            }
            return null;
        }

        public string Name { get; set; }
        public string Mag { get; set; }
    }
}