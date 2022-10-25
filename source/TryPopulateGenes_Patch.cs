using HarmonyLib;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
    [HarmonyPatch(typeof(HumanEmbryo), nameof(HumanEmbryo.TryPopulateGenes))]
    class TryPopulateGenes_Patch
    {
        static void Postfix(ref GeneSet ___geneSet, HumanEmbryo __instance)
        {
            Comp_Clone cloneComp = __instance.TryGetComp<Comp_Clone>();

            if (cloneComp == null || !cloneComp.IsClone) return; // Don't run any of the rest of the patch if the embryo isn't a clone.

            GeneSet copiedGenes = new GeneSet();

            CompHasPawnSources sources = __instance.TryGetComp<CompHasPawnSources>();
            Pawn parent = sources.pawnSources[0];
            if (parent == null)
            {
                Log.Error("Tried to copy genes from the \"parent\" to the clone, but failed to find the parent from CompHasPawnSources.");
                return;
            }

            foreach (Gene gene in parent.genes.Endogenes)
            {
                copiedGenes.AddGene(gene.def);
            }
            
            //TODO: Add in some sort of toggle that makes xenogene cloning optional
            //TODO: Maybe put xenogene cloning behind a research? Need to go more in depth for this, probably using the Comp_Clone to implant the xenogenes (as xenogenes) in the newly-spawned pawn once the embryo is birthed, so that they don't get turned into endogenes.

            copiedGenes.SortGenes();

            ___geneSet = copiedGenes;
        }
    }
}
