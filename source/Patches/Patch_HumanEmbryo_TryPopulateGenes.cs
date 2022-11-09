using HarmonyLib;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
    /// <summary>
    /// Patch that is run as soon as an embryo is spawned (Either through Recipe_ExtractClone or by vanilla fertilization of an ovum). <br />
    /// When run, it checks if the embryo has been marked as a clone, and then performs all of the gene operations relevant to a clone such 
    /// as copying the donor's genes and adding the Clone gene.
    /// </summary>
    [HarmonyPatch(typeof(HumanEmbryo), nameof(HumanEmbryo.TryPopulateGenes))]
    class Patch_HumanEmbryo_TryPopulateGenes
    {
        static void Prefix(ref GeneSet ___geneSet, HumanEmbryo __instance)
        {
            if (!EmbryoTracker.Contains(__instance)) return; // This embryo was not marked as a clone, don't run the rest of the patch.

            //Log.Message("TryPopulateGenes patch detected this embryo was a clone (Tracked in EmbryoTracker)");
            //if (!CloneUtils.HasCloneGene(__instance)) return; // Doesn't work because the Clone gene can't be added externally
            
            GeneSet clonedGenes = new GeneSet();

            CloneUtils.CopyEndogenesFromParent(ref clonedGenes, __instance);

            GeneUtils.ApplyRandomMutations(ref clonedGenes);
            
            clonedGenes.SortGenes();

            ___geneSet = clonedGenes;
            // The vanilla method that runs after this will just do nothing because it first checks if the geneSet field is null, and only runs if so.
        }
    }
}
