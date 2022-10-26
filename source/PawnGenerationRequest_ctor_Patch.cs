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
    /// Patches the PawnGenerationRequest constructor to check for the existence of specific genes, and modifies the request based on the results.
    /// </summary>
    
    // Disabled for now
    //[HarmonyPatch(typeof(PawnGenerationRequest), MethodType.Constructor)]
    class PawnGenerationRequest_ctor_Patch
    {
        void Postfix(PawnGenerationRequest __instance, List<GeneDef> forcedEndogenes, List<GeneDef> forcedXenogenes, ref Gender? ___FixedGender)
        {

        }
    }
}
