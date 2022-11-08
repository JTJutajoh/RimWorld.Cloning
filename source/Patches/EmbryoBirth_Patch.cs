using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Dark.Cloning
{
    /// <summary>
    /// A harmony patch that stores the forced xenogenes for an embryo right before it is born, so that the <see cref="ApplyBirthOutcome_Patch"/> patch can
    /// then get those genes and add them to the request
    /// </summary>
    // This method is the last time the embryo is available before the pawn is generated based on it.
    [HarmonyPatch(typeof(Building_GrowthVat), "EmbryoBirth")]
    public class EmbryoBirth_Patch
    {
        public static void Prefix(Building_GrowthVat __instance)
        {
            if (__instance.selectedEmbryo == null) return;
        }
    }
}
