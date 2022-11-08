using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
    public delegate void DrawSectionDelegate(Rect rect, bool xeno, int count, ref float curY, ref float sectionHeight, Action<int, Rect> drawer, Rect containingRect);

    [HarmonyPatch(typeof(GeneUIUtility), "DrawGeneSections")]
    static class Patch_GeneUIUtility_DrawGeneSections
    {
        static MethodInfo anchorMethod = typeof(Widgets).GetMethod("EndScrollView");

        static DrawSectionDelegate DrawSection = AccessTools.MethodDelegate<DrawSectionDelegate>(AccessTools.Method(typeof(GeneUIUtility), "DrawSection"));

        // endogenesHeight because for some dumb reason vanilla uses xenogenes for embryos even though they're actually endogenes
        //DrawGenesInfo just sums the two together so it shouldn't matter which is used here
        static FieldInfo xenogenesHeight = AccessTools.Field(typeof(GeneUIUtility), "endogenesHeight");
        static FieldInfo scrollHeight = AccessTools.Field(typeof(GeneUIUtility), "scrollHeight");

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(anchorMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg, 1) // Thing target
                    {labels = codes[i].labels}; // Make sure this isn't included in the if block just before the anchor method call
                    yield return new CodeInstruction(OpCodes.Ldarg, 0); // Rect rect
                    yield return new CodeInstruction(OpCodes.Ldloc, 1); // float curY
                    yield return new CodeInstruction(OpCodes.Ldloc, 2); // Rect containingRect

                    yield return CodeInstruction.Call(typeof(Patch_GeneUIUtility_DrawGeneSections), "DrawXenogenesForEmbryo");
                    codes[i].labels = new List<Label>();
                }
                yield return codes[i];
            }
        }

        static void DrawXenogenesForEmbryo(Thing target, Rect rect, float curY, Rect containingRect)
        {
            if (target is HumanEmbryo embryo)
            {
                if (!CloneUtils.HasCloneGene(embryo))
                    return;

                Comp_CloneEmbryo cloneComp = embryo.TryGetComp<Comp_CloneEmbryo>();
                if (cloneComp == null || !cloneComp.IsClone())
                    return;

                // The target is a clone embryo, draw its xenogenes
                List<GeneDef> xenogenes = cloneComp.cloneData.forcedXenogenes.GenesListForReading;

                void drawer(int i, Rect r)
                {
                    Log.Message($"Drawing genedef {xenogenes[i].LabelCap} at {r.x},{r.y} size {r.width},{r.height}");
                    GeneUIUtility.DrawGeneDef(xenogenes[i], r, GeneType.Xenogene, null, true, true, false);
                }

                float tmpXenogenesHeight = (float)xenogenesHeight.GetValue(null);
                DrawSection(rect, true, xenogenes.Count, ref curY, ref tmpXenogenesHeight, drawer, containingRect);
                xenogenesHeight.SetValue(null, tmpXenogenesHeight);

                if (Event.current.type == EventType.Layout)
                {
                    scrollHeight.SetValue(null, curY);
                }
            }
        }
    }
}
