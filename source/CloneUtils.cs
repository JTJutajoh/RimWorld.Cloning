using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Dark.Cloning
{
    public static class CloneUtils
    {
        public static bool HasCloneGene(List<GeneDef> genes)
        {
            if (genes == null || genes.Count < 1) return false;
            return genes.Contains(CloneDefs.Clone);
        }
        public static bool HasCloneGene(GeneSet genes)
        {
            return HasCloneGene(genes.GenesListForReading);
        }
        public static bool HasCloneGene(HumanEmbryo embryo)
        {
            return HasCloneGene(embryo.GeneSet);
        }

        public static void CopyGenesFromParent(ref GeneSet genes, HumanEmbryo embryo)
        {
            CompHasPawnSources sources = embryo.TryGetComp<CompHasPawnSources>();
            Pawn parent = sources.pawnSources[0];
            if (parent == null)
            {
                Log.Error($"Tried to copy genes from the \"parent\" to the clone, but failed to find the parent from CompHasPawnSources on embryo {embryo.ToString()}.");
                return;
            }

            foreach (Gene gene in parent.genes.Endogenes)
            {
                // Safety check to make sure the Clone gene doesn't get inherited (Since we already ensured it's there, this would just add it a second time (We ensured that, right?))
                if (gene.def == CloneDefs.Clone) continue;

                genes.AddGene(gene.def);
            }
        }

        public static CustomXenotype CopyCustomXenotypeFrom(Pawn pawn)
        {
            CustomXenotype result = new CustomXenotype();

            result.name = pawn.genes.xenotypeName;
            result.iconDef = pawn.genes.iconDef;

            return result;
        }
    }
}
