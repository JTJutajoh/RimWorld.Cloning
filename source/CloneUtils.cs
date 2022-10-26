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
    }
}
