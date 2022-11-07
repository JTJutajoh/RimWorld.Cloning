using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Dark.Cloning.Patches
{
    /// <summary>
    /// Patch responsible for allowing cancellation of the job to haul brain scans<br />
    /// It does this by adding another fail condition to the vanilla HaulToContainer JobDriver in a postfix.
    /// </summary>
     
    [HarmonyPatch(typeof(JobDriver_HaulToContainer), "MakeNewToils")]
    public static class JobDriver_HaulToContainer_MakeNewToils_Patch
    {
        static void Postfix(JobDriver_HaulToContainer __instance)
        {
            __instance.FailOn(() => Building_CloneStorageVat.WasHaulJobCanceled(__instance.Container, __instance.ThingToCarry));
        }
    }
}
