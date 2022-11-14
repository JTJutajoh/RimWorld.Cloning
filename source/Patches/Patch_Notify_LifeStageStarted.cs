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
            __state = ( pawn.story.bodyType == BodyTypeDefOf.Child || pawn.story.bodyType == BodyTypeDefOf.Baby );
        }

        static void Postfix(Pawn pawn, bool __state)
        {
            // Clone has grown to 13 (adulthood) and had their body type changed from child. Override it to match their donor parent
            if (__state && pawn.IsClone(out CloneGene cloneGene))
            {
                pawn.story.bodyType = cloneGene.cloneData?.bodyType ?? pawn.story.bodyType;
                /*if (Settings.inheritHair)
                {
                    pawn.story.hairDef = donor.story.hairDef;
                    pawn.style.beardDef = donor.style.beardDef;
                }*/
                pawn.style.Notify_StyleItemChanged();
            }
        }
    }
}
