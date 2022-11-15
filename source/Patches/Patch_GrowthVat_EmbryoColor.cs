using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Dark.Cloning
{
    /// <summary>
    /// Simple patch that overrides vanilla embryo color logic if the embryo is a clone
    /// </summary>     
    [HarmonyPatch(typeof(Building_GrowthVat), "EmbryoColor")]
    static class Patch_GrowthVat_EmbryoColor
    {
        static bool Prefix(Building_GrowthVat __instance, ref Color __result)
        {
            HumanEmbryo humanEmbryo = __instance.selectedEmbryo;
            // Check if this embryo is a clone and only run the logic if so, and do a bunch of nullchecks while we're at it
            if (humanEmbryo != null && humanEmbryo.IsClone(out Comp_CloneEmbryo cloneComp) && cloneComp != null && cloneComp.cloneData != null && cloneComp.cloneData.skinColorOverride != null)
            {
                __result = (Color)cloneComp.cloneData.skinColorOverride;
                return false; // Skip the vanilla method
            }

            return true; // Run the vanilla method
        }
    }
}
