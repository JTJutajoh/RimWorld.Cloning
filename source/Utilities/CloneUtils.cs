using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace Dark.Cloning
{
    public static class CloneUtils
    {
        //TODO: If the devs add a public property, replace this
        static FieldInfo HumanEmbryo_geneSet = AccessTools.Field(typeof(HumanEmbryo), "geneSet");

        public static bool IsClone(this Pawn pawn)
        {
            return pawn.genes.HasGene(CloneDefOf.Clone);
        }
        public static bool IsClone(this Pawn pawn, out CloneGene cloneGene)
        {
            cloneGene = pawn.genes.GetGene(CloneDefOf.Clone) as CloneGene;
            return cloneGene != null;
        }

        public static bool IsClone(this HumanEmbryo embryo)
        {
            var cloneComp = embryo.TryGetComp<Comp_CloneEmbryo>();
            return cloneComp != null && cloneComp.cloneData != null;
        }
        public static bool IsClone(this HumanEmbryo embryo, out Comp_CloneEmbryo cloneComp)
        {
            cloneComp = embryo.TryGetComp<Comp_CloneEmbryo>();
            return cloneComp != null && cloneComp.cloneData != null;
        }

        public static bool IsClonePregnancy(this Hediff hediff)
        {
            var hediffComp = hediff.TryGetComp<HediffComp_Pregnant_Clone>();
            return hediffComp != null && hediffComp.cloneData != null;
        }
        public static bool IsClonePregnancy(this Hediff hediff, out HediffComp_Pregnant_Clone hediffComp)
        {
            hediffComp = hediff.TryGetComp<HediffComp_Pregnant_Clone>();
            return hediffComp != null && hediffComp.cloneData != null;
        }

        [Obsolete]
        public static void CopyEndogenesFromParent(ref GeneSet genes, HumanEmbryo embryo)
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

        public static void CopyEndogenesFrom(Pawn pawn, out GeneSet genes)
        {
            genes = new GeneSet();
            foreach (Gene gene in pawn.genes.Endogenes)
            {
                if (gene.def == CloneDefOf.Clone) continue;

                genes.AddGene(gene.def);
            }
            genes.SortGenes();
        }

        /// <summary>
		/// Modified version of vanilla's ProduceEmbryo. Made static so it can be reused.<br />
        /// Handles marking the produced embryo as a clone so that TryPopulateGenes() correctly adds clone genes
		/// </summary>
        public static HumanEmbryo ProduceCloneEmbryo(Pawn donor, CloneData cloneData)
        {
            HumanEmbryo humanEmbryo = (HumanEmbryo)ThingMaker.MakeThing(ThingDefOf.HumanEmbryo, null);

            if (CloningSettings.CloneRelationship == PawnRelationDefOf.Child)
                humanEmbryo.GetComp<CompHasPawnSources>().AddSource(donor);
            
            EmbryoTracker.Track(humanEmbryo); // Mark this embryo as a clone by adding its hash to a static list stored in EmbryoTracker, to be checked later by harmony patches within HumanEmbryo

            Comp_CloneEmbryo cloneComp = humanEmbryo.TryGetComp<Comp_CloneEmbryo>();
            if (cloneComp == null)
                Log.Error($"HumanEmbryo {humanEmbryo.LabelCap} missing a {nameof(Comp_CloneEmbryo)}. Did a malformed patch from another mod overwrite it?");
            else
                cloneComp.cloneData = cloneData;

            //SOMEDAY: Instead of silently adding the clone gene to embryos, force it in the clone creation dialog
            cloneComp.cloneData.xenogenes.Add(CloneDefOf.Clone);

            // Instead of calling TryPopulateGenes, let's do this manually
            CopyEndogenesFrom(donor, out GeneSet clonedGenes);

            //TODO: If the devs add a public property, replace this
            HumanEmbryo_geneSet.SetValue(humanEmbryo, clonedGenes); 

            return humanEmbryo;
        }

		public static HumanEmbryo ProduceCloneEmbryo(Pawn donor, CustomXenotype customXenotype)
        {
            CloneData cloneData = new CloneData(donor, customXenotype);

            return ProduceCloneEmbryo(donor, cloneData);
        }

        public static HumanEmbryo ProduceCloneEmbryo(Pawn donor, XenotypeDef xenotypeDef)
        {
            CloneData cloneData = new CloneData(donor, xenotypeDef);

            return ProduceCloneEmbryo(donor, cloneData);
        }

        public static CloneData GetCloneDataFromBirtherThing(Thing birtherThing)
        {
            CloneData cloneData = null;
            // GrowthVats keep the embryo Thing in their innerContainer until birth
            if (birtherThing is Building_GrowthVat growthVat)
            {
                HumanEmbryo embryo = growthVat.selectedEmbryo;
                if (embryo.IsClone(out Comp_CloneEmbryo cloneComp))
                    cloneData = cloneComp.cloneData;
            }
            // Pregnant pawns do not keep the embryo Thing and instead transfer its data to a series of hediffs
            else if (birtherThing is Pawn birtherPawn)
            {
                Hediff hediff = birtherPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLaborPushing);
                if (hediff.IsClonePregnancy(out HediffComp_Pregnant_Clone cloneComp))
                    cloneData = cloneComp.cloneData;
            }
            return cloneData;
        }

        public static void AddCloneRelation(Pawn clone, Pawn donor)
        {
            if (clone == null)
            {
                Log.Error("Error applying clone relation, pawn was null");
                return;
            }
            if (donor != null && CloningSettings.CloneRelationship != null && CloningSettings.CloneRelationship != PawnRelationDefOf.Child)
            {
                donor.relations.AddDirectRelation(CloningSettings.CloneRelationship, clone);
            }
            else if (donor == null)
            {
                Log.Warning($"Tried to add clone relation for new clone {clone.LabelCap}, but cloneData had null donor reference. Did the donor pawn get cleaned up?");
            }
        }

        public static void ApplyAppearanceToClone(Pawn pawn, CloneData cloneData)
        {
            if (pawn == null)
            {
                Log.Error("Error applying clone appearance, pawn was null");
                return;
            }
            if (cloneData == null)
            {
                Log.Error("Error applying clone appearance, cloneData was null");
                return;
            }
            //TODO: Add checks for ideology if necessary, like for beards
            pawn.story.headType = cloneData.headType;
            pawn.story.skinColorOverride = cloneData.skinColorOverride;
            pawn.story.furDef = cloneData.furDef;
            if (CloningSettings.inheritHair)
            {
                pawn.story.hairDef = cloneData.hairDef;
                pawn.style.beardDef = cloneData.beardDef;
            }
            pawn.style.Notify_StyleItemChanged();
        }

        public static void ApplyBodyTypeToClone(Pawn pawn)
        {
            if (pawn.IsClone(out CloneGene cloneGene))
            {
                pawn.story.bodyType = cloneGene.cloneData?.bodyType ?? pawn.story.bodyType; // Null coalesce in case the clonegene doesn't contain valid bodytype data for some reason
                pawn.style.Notify_StyleItemChanged();
            }
        }
    }
}
