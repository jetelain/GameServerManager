using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BIS.Core.Config;

namespace Arma3ServerToolbox.ArmaPersist
{
    public class PersistBackup
    {
        public PersistBackup()
        {
        }

        public PersistBackup (string name, DateTime dt)
        {
            this.Name = name;
            this.LastChange = dt;
        }

        public List<PersistPlayer> Players { get; set; } = new List<PersistPlayer>();
        public List<PersistBox> Boxes { get; set; } = new List<PersistBox>();
        public List<PersistVehicle> Vehicles { get; set; } = new List<PersistVehicle>();

        public string Name { get; set; }
        public DateTime LastChange { get; set; }

        public static List<PersistBackup> Read(Stream stream, DateTime dt)
        {
            var file = new ParamFile(stream);
            var profile = file.Root.Entries.OfType<ParamClass>().FirstOrDefault(c => c.Name == "ProfileVariables");
            var backupEntries = profile.Entries.OfType<ParamClass>().Where(e => ((e.Entries.OfType<ParamValue>().FirstOrDefault(e => e.Name == "name").Value.Value as string) ?? "").StartsWith("gtd_persistence:")).ToList();
            var backups = new List<PersistBackup>();
            foreach (var backupEntry in backupEntries)
            {
                var backupData = ToArray(backupEntry.Entries.OfType<ParamClass>().FirstOrDefault(e => e.Name == "data"));
                var backupName = (string)backupEntry.Entries.OfType<ParamValue>().FirstOrDefault(e => e.Name == "name").Value.Value;
                var backup = new PersistBackup(backupName.Substring(16), dt);
                foreach (List<object> playerData in (List<object>)backupData[0])
                {
                    backup.Players.Add(new PersistPlayer(playerData));
                }
                int id = 0;
                foreach (List<object> boxData in (List<object>)backupData[1])
                {
                    id++;
                    if (boxData != null)
                    {
                        backup.Boxes.Add(new PersistBox(boxData, id));
                    }
                }
                id = 0;
                foreach (List<object> vehicleData in (List<object>)backupData[2])
                {
                    id++;
                    if (vehicleData != null)
                    {
                        backup.Vehicles.Add(new PersistVehicle(vehicleData, id));
                    }
                }
                backups.Add(backup);
            }
            return backups;
        }

        private static List<object> ToArray(ParamClass paramClass)
        {
            var result = new List<object>();

            if (paramClass.Entries.Count == 1)
            {
                return result;
            }

            var content = (ParamClass)paramClass.Entries[1];
            foreach(var entry in content.Entries.OfType<ParamClass>())
            {
                var entryData = (ParamClass)entry.Entries[0];
                if (entryData.Entries[0] is ParamValue)
                {
                    result.Add(null);
                }
                else
                {
                    var type = ((ParamArray)((ParamClass)entryData.Entries[0]).Entries[0]).Array.Entries[0].Value as string;
                    switch (type)
                    {
                        case "ARRAY":
                            result.Add(ToArray(entryData));
                            break;
                        case "STRING":
                            result.Add((string)((ParamValue)entryData.Entries[1]).Value.Value);
                            break;
                        case "SCALAR":
                            result.Add((float)((ParamValue)entryData.Entries[1]).Value.Value);
                            break;
                        case "BOOL":
                            result.Add(((int)((ParamValue)entryData.Entries[1]).Value.Value) != 0);
                            break;
                        case "NOTHING":
                            result.Add(null);
                            break;
                        default:
                            break;
                    }
                }
            }
            return result;
        }
    }
}
