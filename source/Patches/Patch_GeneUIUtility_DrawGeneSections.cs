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
                /*
                if (Event.current.type == EventType.Layout)
                {
	                GeneUIUtility.scrollHeight = num;
                }
                */
                if (codes[i].Calls(anchorMethod)) //Widgets.EndScrollView();
                {
                    yield return new CodeInstruction(OpCodes.Ldarg, 1) // Thing target
                    // Make sure this isn't included in the if block just before the anchor method call by changing the IL labels to match the label of the anchor method (Since it is outside of the if block)
                    { labels = codes[i].labels}; 
                    yield return new CodeInstruction(OpCodes.Ldarg, 0); // Rect rect
                    yield return new CodeInstruction(OpCodes.Ldloc, 1); // float curY
                    yield return new CodeInstruction(OpCodes.Ldloc, 2); // Rect containingRect

                    yield return CodeInstruction.Call(typeof(Patch_GeneUIUtility_DrawGeneSections), "DrawXenogenesForEmbryo");
                    // Clear the labels for the next instruction so we don't end up with a duplicate label. It doesn't break without this, but just to be technically correct
                    codes[i].labels = new List<Label>();
                }
                yield return codes[i];
                /*
                Widgets.EndScrollView();
	            GUI.EndGroup();
                */
            }
        }
        /* Original (unpatched Vanilla 1.4) method:
        private static void DrawGeneSections(Rect rect, Thing target, GeneSet genesOverride, ref Vector2 scrollPosition)
		{
			GeneUIUtility.RecacheGenes(target, genesOverride);
			GUI.BeginGroup(rect);
			Rect rect2 = new Rect(0f, 0f, rect.width - 16f, GeneUIUtility.scrollHeight);
			float num = 0f;
			Widgets.BeginScrollView(rect.AtZero(), ref scrollPosition, rect2, true);
			Rect containingRect = rect2;
			containingRect.y = scrollPosition.y;
			containingRect.height = rect.height;
			if (target is Pawn)
			{
				if (GeneUIUtility.endogenes.Any<Gene>())
				{
					GeneUIUtility.DrawSection(rect, false, GeneUIUtility.endogenes.Count, ref num, ref GeneUIUtility.endogenesHeight, delegate(int i, Rect r)
					{
						GeneUIUtility.DrawGene(GeneUIUtility.endogenes[i], r, GeneType.Endogene, true, true);
					}, containingRect);
					num += 12f;
				}
				GeneUIUtility.DrawSection(rect, true, GeneUIUtility.xenogenes.Count, ref num, ref GeneUIUtility.xenogenesHeight, delegate(int i, Rect r)
				{
					GeneUIUtility.DrawGene(GeneUIUtility.xenogenes[i], r, GeneType.Xenogene, true, true);
				}, containingRect);
			}
			else
			{
				GeneType geneType = (genesOverride != null || target is HumanEmbryo) ? GeneType.Endogene : GeneType.Xenogene;
				GeneUIUtility.DrawSection(rect, geneType == GeneType.Xenogene, GeneUIUtility.geneDefs.Count, ref num, ref GeneUIUtility.xenogenesHeight, delegate(int i, Rect r)
				{
					GeneUIUtility.DrawGeneDef(GeneUIUtility.geneDefs[i], r, geneType, null, true, true, false);
				}, containingRect);
			}
			if (Event.current.type == EventType.Layout)
			{
				GeneUIUtility.scrollHeight = num;
			}
			Widgets.EndScrollView();
			GUI.EndGroup();
		}
         */

        /// <summary>
        /// Injected method, checks if the thing whose genes are being drawn is a clone embryo first,
        /// and then if it is it calls GeneUIUtility.DrawSection (Using the static delegate <see cref="DrawSection"/>) 
        /// to draw them the same way as vanilla
        /// </summary>
        static void DrawXenogenesForEmbryo(Thing target, Rect rect, float curY, Rect containingRect)
        {
            if (target is HumanEmbryo embryo)
            {
                if (!embryo.IsClone(out Comp_CloneEmbryo cloneComp))
                    return;

                // The target is a clone embryo, draw its xenogenes
                List<GeneDef> xenogenes = cloneComp.cloneData.forcedXenogenes.GenesListForReading;

                void drawer(int i, Rect r) =>
                    GeneUIUtility.DrawGeneDef(xenogenes[i], r, GeneType.Xenogene, null, true, true, false);

                // Store it in a tmp var because you can't pass the value of a FieldInfo by ref
                float tmpXenogenesHeight = (float)xenogenesHeight.GetValue(null);
                DrawSection(rect, true, xenogenes.Count, ref curY, ref tmpXenogenesHeight, drawer, containingRect);
                // Apply the tmp value that was passed by ref to the original field
                xenogenesHeight.SetValue(null, tmpXenogenesHeight);

                // This was already called before in the vanilla method, but it should cause no harm to call it again. Will simply override the new value of scrollHeight
                if (Event.current.type == EventType.Layout)
                {
                    scrollHeight.SetValue(null, curY);
                }
            }
        }
    }
}
