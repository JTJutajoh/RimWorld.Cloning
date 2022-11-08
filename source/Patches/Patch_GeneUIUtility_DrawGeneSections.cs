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
    [HarmonyPatch(typeof(GeneUIUtility), "DrawGeneSections")]
    class Patch_GeneUIUtility_DrawGeneSections
    {
        static MethodInfo anchorMethod = typeof(Widgets).GetMethod("EndScrollView");

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(anchorMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg, 1); // Thing target
                    yield return new CodeInstruction(OpCodes.Ldarg, 0); // Rect rect
                    yield return new CodeInstruction(OpCodes.Ldloc, 1); // float curY
                    yield return new CodeInstruction(OpCodes.Ldloc, 2); // Rect containingRect

                    yield return CodeInstruction.Call(typeof(Patch_GeneUIUtility_DrawGeneSections), "DrawXenogenesForEmbryo");
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

                MethodInfo DrawSection = AccessTools.Method(typeof(GeneUIUtility), "DrawSection");

                void drawer(int i, Rect r)
                {
                    Log.Message($"Drawing genedef {xenogenes[i].LabelCap} at {r.x},{r.y} size {r.width},{r.height}");
                    Widgets.Label(r, $"Gene: {xenogenes[i]}");
                    GeneUIUtility.DrawGeneDef(xenogenes[i], r, GeneType.Xenogene, null, true, true, false);
                }
                Action<int, Rect> drawAction = drawer;

                FieldInfo xenogenesHeight = AccessTools.Field(typeof(GeneUIUtility), "endogenesHeight");

                object[] args = new object[7]
                {
                    rect,
                    true,
                    xenogenes.Count,
                    curY,
                    xenogenesHeight.GetValue(null),
                    drawAction,
                    containingRect
                };
                DrawSection.Invoke(null, args);
                // Update the values that were passed by ref
                //curY
                curY = (float)args[3];
                //xenogenesHeight
                xenogenesHeight.SetValue(null, args[4]);

                if (Event.current.type == EventType.Layout)
                {
                    FieldInfo scrollHeight = AccessTools.Field(typeof(GeneUIUtility), "scrollHeight");
                    scrollHeight.SetValue(null, curY);
                }
            }
        }
    }
}
