﻿using System;
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
            return genes.Contains(CloneDefOf.Clone);
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
            return pawn.genes.HasGene(CloneDefOf.Clone);
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
                if (gene.def == CloneDefOf.Clone) continue;

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
		public static HumanEmbryo ProduceCloneEmbryo(Pawn donor, GeneSet forcedXenogenes = null)
        {
            HumanEmbryo humanEmbryo = (HumanEmbryo)ThingMaker.MakeThing(ThingDefOf.HumanEmbryo, null);

            humanEmbryo.GetComp<CompHasPawnSources>().AddSource(donor);
            
            EmbryoTracker.Track(humanEmbryo); // Mark this embryo as a clone by adding its hash to a static list stored in EmbryoTracker, to be checked later by harmony patches within HumanEmbryo

            CloneData cloneData = new CloneData(forcedXenogenes);

            Comp_CloneEmbryo cloneComp = humanEmbryo.TryGetComp<Comp_CloneEmbryo>();
            if (cloneComp == null)
                Log.Error($"HumanEmbryo {humanEmbryo.LabelCap} missing a {nameof(Comp_CloneEmbryo)}. Did a malformed patch from another mod overwrite it?");
            else
            {
                cloneComp.cloneData = cloneData;
            }
            
            humanEmbryo.TryPopulateGenes();

            return humanEmbryo;
        }

        public static HediffComp_Pregnant_Clone GetCloneHediffCompFromPregnancy(Pawn pawn)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman);
            if (hediff == null)
            {
                hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLabor);
            }
            if (hediff == null)
            {
                hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLaborPushing);
            }
            if (hediff == null) 
                Log.Error($"Unable to find any pregnancy hediff on pawn {pawn.LabelCap}");

            HediffComp_Pregnant_Clone cloneComp = hediff.TryGetComp<HediffComp_Pregnant_Clone>();
            if (cloneComp == null)
                Log.Error($"Error getting the {nameof(HediffComp_Pregnant_Clone)} from the pregnancy hediff");

            return cloneComp;
        }

        //TODO: Remove this probably
        public static void StartXenogermReplicatingHediff(Pawn pawn)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.XenogermReplicating);

            hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = Mathf.RoundToInt(60000f * CloningSettings.CloneExtractorRegrowingDurationDaysRange.RandomInRange);
        }
    }
}
