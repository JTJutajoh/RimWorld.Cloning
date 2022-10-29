using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Dark.Cloning.Patches
{
    [HarmonyPatch(typeof(LifeStageWorker_HumanlikeAdult), methodName: "Notify_LifeStageStarted")]
    class Notify_LifeStageStarted_Patch
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
            if (__state && CloneUtils.HasCloneGene(pawn))
            {
                // Get the clone's parent by getting their parent of the same gender, since we force clones to match donor genders.
                //FIXME: This will cause an incompatibility with any mods that change gender if the donor's gender changes before the clone reaches 13. Not sure how to approach fixing this. Maybe it doesn't need fixing?
                Pawn donor = pawn.gender == Gender.Female ? pawn.GetMother() : pawn.GetFather(); 
                pawn.story.bodyType = donor.story.bodyType;
                pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
            }
        }
    }
}
