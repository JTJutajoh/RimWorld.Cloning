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
            if (!EmbryoTracker.Contains(__instance)) return; // This embryo was not marked as a clone, don't run the rest of the patch.

            //Log.Message("TryPopulateGenes patch detected this embryo was a clone (Tracked in EmbryoTracker)");
            //if (!CloneUtils.HasCloneGene(__instance)) return; // Doesn't work because the Clone gene can't be added externally
            
            GeneSet clonedGenes = new GeneSet();
            clonedGenes.AddGene(CloneDefs.Clone);

            CopyGenesFromParent(ref clonedGenes, __instance);
            
            //TODO: Add in some sort of toggle that makes xenogene cloning optional
            //TODO: Maybe put xenogene cloning behind a research? Need to go more in depth for this, probably using the Comp_Clone to implant the xenogenes (as xenogenes) in the newly-spawned pawn once the embryo is birthed, so that they don't get turned into endogenes.

            clonedGenes.SortGenes();

            ___geneSet = clonedGenes;
        }

        static void CopyGenesFromParent(ref GeneSet genes, HumanEmbryo embryo)
        {
            //Log.Message("Copying genes from the parent");
            CompHasPawnSources sources = embryo.TryGetComp<CompHasPawnSources>();
            Pawn parent = sources.pawnSources[0];
            if (parent == null)
            {
                Log.Error("Tried to copy genes from the \"parent\" to the clone, but failed to find the parent from CompHasPawnSources.");
                return;
            }

            foreach (Gene gene in parent.genes.Endogenes)
            {
                // Safety check to make sure the Clone gene doesn't get inherited (Since we already ensured it's there, this would just add it a second time)
                if (gene.def == CloneDefs.Clone) continue;

                genes.AddGene(gene.def);
            }
        }
    }
}
