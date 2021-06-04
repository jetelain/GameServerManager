using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arma3ServerToolbox.ArmaPersist
{
    public static class ItemLabelHelper
    {
        private static readonly Dictionary<string, string> labels = new Dictionary<string, string>()
        {
            {"rhs_mag_30Rnd_556x45_M855_PMAG_Tan",             "Mag 30Rnd 5.56x45"},
            {"rhs_mag_30Rnd_556x45_M855A1_Stanag",             "Mag 30Rnd 5.56x45"},
            {"rhs_mag_30Rnd_556x45_M855_PMAG_Tan_Tracer_Red",  "Mag 30Rnd 5.56x45 Tracer Red"},
            {"OFrP_17Rnd_9x19_Glock17",                        "Mag 17Rnd 9x19"},
            {"ACE_MapTools",                                   "Map Tools"},
            {"ACE_EarPlugs",                                   "Ear Plugs"},
            {"ACE_IR_Strobe_Item",                             "IR Strobe"},
            {"ACE_fieldDressing",                              "Field Dressing"},
            {"ACE_packingBandage",                             "Packing Bandage"},
            {"ACE_elasticBandage",                             "Elastic Bandage"},
            {"ACE_tourniquet",                                 "Tourniquet"},
            {"amf_felin_cover_tan",                            "FELIN Helmet TAN"},
            {"GTD_OpsC_1",                                     "OpsCore Helmet"},
            {"GTD_OpsC_2",                                     "OpsCore Helmet"},
            {"GTD_OpsC_3",                                     "OpsCore Helmet"},
            {"GTD_OpsC_4",                                     "OpsCore Helmet"},
            {"ACRE_PRC152",                                    "AN/PRC-152"},
            {"ACRE_PRC343",                                    "AN/PRC-343"},
            {"ACRE_PRC117F",                                   "AN/PRC-117F"},
            {"AMF_FELIN_BACKPACK_TAN",                         "FELIN Backpack TAN"},
            {"GTD_GILET_IV",                                   "SMB TL"},
            {"rhsusf_weap_m9",                                 "M9"}
        };

        public static string GetLabel(string name, string prefix = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }
            string label;
            if (labels.TryGetValue(name, out label))
            {
                return prefix + label;
            }
            label = name;
            label = label.Replace("OFrP_", " ", StringComparison.OrdinalIgnoreCase);
            label = label.Replace("ACE_", " ", StringComparison.OrdinalIgnoreCase);
            label = label.Replace("GTD_", " ", StringComparison.OrdinalIgnoreCase);
            label = label.Replace("rhs_", " ", StringComparison.OrdinalIgnoreCase);
            label = label.Replace("CUP_", " ", StringComparison.OrdinalIgnoreCase);
            label = label.Replace("CFP_", " ", StringComparison.OrdinalIgnoreCase);
            label = label.Replace("AMF_", " ", StringComparison.OrdinalIgnoreCase);
            label = label.Replace("_", " ");
            return prefix + label;
        }
    }
}
