using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Dark.Cloning
{
    [HarmonyPatch(typeof(LifeStageWorker_HumanlikeAdult), methodName: "Notify_LifeStageStarted")]
    class Patch_Notify_LifeStageStarted
    {
        /// <summary>
        /// Checks if the pawn is a child or baby before the vanilla method updates the body type,
        /// so that the postfix patch can use that info to (conditionally) override it.
        /// </summary>
        /// <param name="__state">Bool storing whether or not the pawn was a baby/child, and should have its new body type overridden.</param>
        static void Prefix(Pawn pawn, ref bool __state)
        {
            // Check if the body type of the pawn before aging up is that of a child or baby, and store the result in a state variable to be checked in the postfix after vanilla ages them up normally
            __state = ( pawn.story.bodyType == BodyTypeDefOf.Child || pawn.story.bodyType == BodyTypeDefOf.Baby ); //HACK: There's probably a better way to detect if they should get an adult body type
        }

        static void Postfix(Pawn pawn, bool __state)
        {
            if (!__state)
                return;
            // Clone has grown to 13 (adulthood) and had their body type changed from child. Override it to match their donor parent
            if (pawn.IsClone(out CloneGene cloneGene))
            {
                cloneGene.cloneData.ApplyBodyType(pawn);
            }
        }
    }
}
