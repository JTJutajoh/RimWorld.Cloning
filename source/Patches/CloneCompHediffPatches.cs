using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using HarmonyLib;
using Verse;

namespace Dark.Cloning
{
    /// <summary>
    /// Patch that fetches the <see cref="CloneData"/> from the <see cref="Comp_CloneEmbryo"/> and passes it along to the newly-created <see cref="HediffComp_Pregnant_Clone"/> on the pregnant hediff.
    /// </summary>
    [HarmonyPatch(typeof(Recipe_ImplantEmbryo), nameof(Recipe_ImplantEmbryo.ApplyOnPawn))]
    public class Recipe_ImplantEmbryo_ApplyOnPawn_Patch
    {
        static void Prefix(ref CloneData __state, List<Thing>ingredients)
        {
            //SOMEDAY: Maybe find an alternative to this, since this patch causes it to do this search twice
            HumanEmbryo embryo = (HumanEmbryo)ingredients.FirstOrDefault((Thing t) => t.def == ThingDefOf.HumanEmbryo);
            
            if (!embryo.IsClone(out Comp_CloneEmbryo cloneComp)) return;

            __state = cloneComp?.cloneData;
        }

        static void Postfix(ref CloneData __state, Pawn pawn)
        {
            if (__state == null) return;

            Hediff pregnantHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman);
            var cloneComp = pregnantHediff.TryGetComp<HediffComp_Pregnant_Clone>();
            if (cloneComp != null)
            {
                cloneComp.cloneData = __state;
            }
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
            HediffComp_Pregnant_Clone oldComp = __instance.TryGetComp<HediffComp_Pregnant_Clone>();

            //Hediff hediff = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLabor);
            Hediff_Labor hediff_Labor = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLabor) as Hediff_Labor;
            HediffComp_Pregnant_Clone newComp = hediff_Labor.TryGetComp<HediffComp_Pregnant_Clone>();

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
            HediffComp_Pregnant_Clone oldComp = __instance.TryGetComp<HediffComp_Pregnant_Clone>();

            Hediff_LaborPushing hediff_LaborPushing = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLaborPushing) as Hediff_LaborPushing;
            HediffComp_Pregnant_Clone newComp = hediff_LaborPushing.TryGetComp<HediffComp_Pregnant_Clone>();

            newComp.cloneData = oldComp.cloneData;
        }
    }
}
