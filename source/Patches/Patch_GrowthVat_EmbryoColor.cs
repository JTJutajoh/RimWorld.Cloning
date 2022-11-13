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
            bool skipVanilla = false;

            HumanEmbryo humanEmbryo = __instance.selectedEmbryo;
            // Check if this embryo is a clone and only run the logic if so
            if (humanEmbryo != null && humanEmbryo.IsClone(out Comp_CloneEmbryo cloneComp) && cloneComp != null && cloneComp.cloneData != null && cloneComp.cloneData.skinColorOverride != null)
            {
                __result = cloneComp.cloneData.skinColorOverride ?? Color.white;
                skipVanilla = true;
            }

            return !skipVanilla;
        }
    }
}
