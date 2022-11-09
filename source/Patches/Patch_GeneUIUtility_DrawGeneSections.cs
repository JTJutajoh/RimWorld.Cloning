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
    // Delegate used here since the DrawSection method is private and cannot be called directly. Cleaner and more performant than using AccessTools everytime
    public delegate void DrawSectionDelegate(Rect rect, bool xeno, int count, ref float curY, ref float sectionHeight, Action<int, Rect> drawer, Rect containingRect);

    /// <summary>
    /// Patch that adds xenogenes to the ITab displaying the genes of an embryo.<br />
    /// Transpiler injects a method call just before the end of the scrollview, and calls GeneUIUtility.DrawSection (A private method)
    /// to draw the xenogenes contained in the embryo's cloneComp (if any).
    /// </summary>
    [HarmonyPatch(typeof(GeneUIUtility), "DrawGeneSections")]
    static class Patch_GeneUIUtility_DrawGeneSections
    {
        static MethodInfo anchorMethod = typeof(Widgets).GetMethod("EndScrollView");

        /// <summary>
        /// Statically bind the reference of the above delegate <see cref="DrawSectionDelegate"/> to the private method DrawSection
        /// </summary>
        static DrawSectionDelegate DrawSection = AccessTools.MethodDelegate<DrawSectionDelegate>(AccessTools.Method(typeof(GeneUIUtility), "DrawSection"));

        // endogenesHeight because for some dumb reason vanilla uses xenogenes for embryos even though they're actually endogenes
        //GeneUIUtility.DrawGenesInfo just sums the two together so it shouldn't matter which is used here, as long as it's the opposite of what vanilla uses for embryo endogenes
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
                    // Make sure this isn't included in the if block just before the anchor method call, by changing the IL labels to match the label of the anchor method (Since it is outside of the if block)
                    { labels = codes[i].labels}; 
                    yield return new CodeInstruction(OpCodes.Ldarg, 0); // Rect rect
                    yield return new CodeInstruction(OpCodes.Ldloc, 1); // float curY
                    yield return new CodeInstruction(OpCodes.Ldloc, 2); // Rect containingRect

                    yield return CodeInstruction.Call(typeof(Patch_GeneUIUtility_DrawGeneSections), "DrawXenogenesForEmbryo");
                    // Clear the labels for the next instruction so we don't end up with a duplicate label. It doesn't break without this, but just to be technically correct
                    codes[i].labels = new List<Label>();
                }
                yield return codes[i];
            }
        }

        /// <summary>
        /// Injected method, checks if the thing whose genes are being drawn is a clone embryo first,
        /// and then if it is it calls GeneUIUtility.DrawSection (Using the static delegate <see cref="DrawSection"/>) 
        /// to draw them the same way as vanilla
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rect"></param>
        /// <param name="curY"></param>
        /// <param name="containingRect"></param>
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
