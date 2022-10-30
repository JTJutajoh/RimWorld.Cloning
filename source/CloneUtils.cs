using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

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
        public static bool HasCloneGene(Pawn pawn)
        {
            return pawn.genes.HasGene(CloneDefs.Clone);
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

        /// <summary>
		/// Modified version of vanilla's ProduceEmbryo. Made static so it can be reused. <br />
        /// Handles marking the produced embryo as a clone so that TryPopulateGenes() correctly adds clone genes
		/// </summary>
		public static Thing ProduceCloneEmbryo(Pawn donor)
        {
            HumanEmbryo humanEmbryo = (HumanEmbryo)ThingMaker.MakeThing(ThingDefOf.HumanEmbryo, null);

            humanEmbryo.GetComp<CompHasPawnSources>().AddSource(donor);

            EmbryoTracker.Track(humanEmbryo); // Mark this embryo as a clone by adding its hash to a static list stored in EmbryoTracker, to be checked later by harmony patches within HumanEmbryo
                                              //Log.Message("Embryo added to EmbryoTracker, calling TryPopulateGenes...");
            humanEmbryo.TryPopulateGenes();

            return humanEmbryo;
        }

        //TODO: Remove this probably
        public static void StartXenogermReplicatingHediff(Pawn pawn)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.XenogermReplicating);

            hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = Mathf.RoundToInt(60000f * Settings.CloneExtractorRegrowingDurationDaysRange.RandomInRange);
        }
    }
}
