using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using HarmonyLib;
using Verse;

/*
 * A set of patches responsible for passing on the clone comps as the pregnancy hediff progresses
 * The hediffcomp is patched in xml to be included on all pregnancies, but with a default null cloneData reference.
 * When an embryo is implanted into a pawn, these harmony patches fetch the clone comp on the new hediffs and pass the cloneData from the embryo's clone comp on to them
 * And then when the hediff progresses to the following stages of pregnancy (stages are: Pregnant, Labor, LaborPushing), these patches ensure the cloneData gets passed on to their clone comps
 * The ApplyBirthOutcome patch then fetches the cloneData from the LaborPushing hediff and applies it to the newly-generated pawn
 */
namespace Dark.Cloning
{
    /// <summary>
    /// Patch that fetches the <see cref="CloneData"/> from the <see cref="Comp_CloneEmbryo"/> and passes it along to the newly-created <see cref="HediffComp_Pregnant_Clone"/> on the pregnant hediff.
    /// This is the most complex of the patches because it has to pass data between two patch methods
    /// </summary>
    [HarmonyPatch(typeof(Recipe_ImplantEmbryo), nameof(Recipe_ImplantEmbryo.ApplyOnPawn))]
    public class Recipe_ImplantEmbryo_ApplyOnPawn_Patch
    {
        static CloneData staticCloneData;
        /// <summary>
        /// Prefix that fetches the cloneData from the embryo that was used in the implantation recipe and stores it for the postfix to apply to the hediffComp
        /// </summary>
        /// <param name="ingredients">List of ingredients for the implant recipe, which will include the embryo</param>
        static void Prefix(List<Thing>ingredients)
        {
            HumanEmbryo embryo = (HumanEmbryo)ingredients.FirstOrDefault((Thing t) => t.def == ThingDefOf.HumanEmbryo);
            
            if (!embryo.IsClone(out Comp_CloneEmbryo cloneComp)) // If this embryo wasn't a clone, don't run the rest of the patch
                return;

            staticCloneData = cloneComp?.cloneData;
        }

        /// <summary>
        /// Once the implant surgery is complete, this postfix runs and applies the previously-stored CloneData to the new pregnancy hediff, if there is any.
        /// </summary>
        /// <param name="pawn"></param>
        static void Postfix(Pawn pawn)
        {
            if (staticCloneData == null) // If this pregnancy isn't a clone, don't run the patch
                return;

            var pregnantHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman);
            if (pregnantHediff == null) // This shouldn't ever happen, but maybe some other mod replaced the pregnancy hediff or something
            {
                Log.Error($"Error passing on CloneData to pregnancy on {pawn.LabelCap}, no hediff of def PregnantHuman found.");
                return;
            }

            var cloneComp = pregnantHediff.TryGetComp<HediffComp_Pregnant_Clone>();
            if (cloneComp != null)
            {
                cloneComp.cloneData = staticCloneData;
            }
            else
                Log.Error($"Error getting {nameof(HediffComp_Pregnant_Clone)} on {pawn.LabelCap}'s pregnancy. Did a malformed patch from another mod overwrite it?");
        }
    }

    /// <summary>
    /// Passes on <see cref="CloneData"/> from the pregnancy hediff to the labor hediff
    /// </summary>
    [HarmonyPatch(typeof(Hediff_Pregnant), nameof(Hediff_Pregnant.StartLabor))]
    public class Hediff_Pregnant_StartLabor_Patch
    {
        static void Postfix(Hediff_Pregnant __instance)
        {
            var oldComp = __instance.TryGetComp<HediffComp_Pregnant_Clone>();

            //Hediff hediff = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLabor);
            var hediff_Labor = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLabor) as Hediff_Labor;
            var newComp = hediff_Labor.TryGetComp<HediffComp_Pregnant_Clone>();

            newComp.cloneData = oldComp.cloneData;
        }
    }

    /// <summary>
    /// Passes on <see cref="CloneData"/> from the labor hediff to the labor pushing hediff
    /// </summary>
    [HarmonyPatch(typeof(Hediff_Labor), nameof(Hediff_Labor.PreRemoved))]
    public class Hediff_Labor_PreRemoved_Patch
    {
        static void Postfix(Hediff_Labor __instance)
        {
            var oldComp = __instance.TryGetComp<HediffComp_Pregnant_Clone>();

            var hediff_LaborPushing = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLaborPushing) as Hediff_LaborPushing;
            var newComp = hediff_LaborPushing.TryGetComp<HediffComp_Pregnant_Clone>();

            newComp.cloneData = oldComp.cloneData;
        }
    }
}
