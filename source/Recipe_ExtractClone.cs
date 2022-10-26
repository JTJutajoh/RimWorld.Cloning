using System.Collections.Generic;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
	/// <summary>
	/// Modified version of vanilla Recipe_ExtractOvum class, removing restrictions and marking the resulting embryo as a clone.
	/// </summary>
    public class Recipe_ExtractClone : Recipe_AddHediff
    {
		// Copied directly from vanilla, with irrelevant conditions commented out
		public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
		{
			if (!Find.Storyteller.difficulty.ChildrenAllowed)
			{
				return false;
			}
			Pawn pawn;
			if (( pawn = ( thing as Pawn ) ) == null)
			{
				return false;
			}
			/*if (pawn.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman, false))
			{
				return "CannotPregnant".Translate();
			}*/
			if (pawn.ageTracker.AgeBiologicalYears < this.recipe.minAllowedAge)
			{
				return "CannotMustBeAge".Translate(this.recipe.minAllowedAge);
			}
			/*if (pawn.Sterile(false))
			{
				return "CannotSterile".Translate();
			}*/
			if (pawn.health.hediffSet.HasHediff(HediffDefOf.OvumExtracted, false))
			{
				return "SurgeryDisableReasonOvumExtracted".Translate();
			}
			return base.AvailableReport(thing, part);
		}

		// Copied directly from vanilla
		public override bool CompletableEver(Pawn surgeryTarget)
		{
			return base.IsValidNow(surgeryTarget, null, true);
		}

		// Similar to the version in Recipe_ExtractOvum but calls custom methods for clone behavior
		protected override void OnSurgerySuccess(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			//TODO: See if we can just skip the ovum altogether.
			HumanOvum ovum = ThingMaker.MakeThing(ThingDefOf.HumanOvum, null) as HumanOvum;
			ovum.TryGetComp<CompHasPawnSources>().AddSource(pawn);

			Thing embryo = ProduceCloneEmbryo(pawn, ovum);

			StartXenogermReplicatingHediff(pawn);

			if (!GenPlace.TryPlaceThing(embryo, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, (IntVec3 x) => x.InBounds(pawn.Map) && x.Standable(pawn.Map) && !x.Fogged(pawn.Map), default(Rot4)))
			{
				Log.Error("Could not drop embryo near " + pawn.Position);
			}
		}

		private void StartXenogermReplicatingHediff(Pawn pawn)
        {
			Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.XenogermReplicating);

			hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = Settings.cloningCooldownDays * 60000;
        }

		/// <summary>
		/// Modified version of vanilla's ProduceEmbryo, gets the custom Comp_Clone from the created embryo and sets it to be a clone BEFORE calling TryPopulateGenes
		/// </summary>
		//TODO: Roll ProduceCloneEmbryo into a transpiler instead?
		private Thing ProduceCloneEmbryo(Pawn father, HumanOvum ovum)
        {
			HumanEmbryo humanEmbryo = (HumanEmbryo)ThingMaker.MakeThing(ThingDefOf.HumanEmbryo, null);
			CompHasPawnSources comp = humanEmbryo.GetComp<CompHasPawnSources>();
			List<Pawn> pawnSources = ovum.GetComp<CompHasPawnSources>().pawnSources;
			if (!pawnSources.NullOrEmpty<Pawn>())
			{
				foreach (Pawn current in pawnSources)
				{
					comp.AddSource(current);
				}
			}
			comp.AddSource(father);

			EmbryoTracker.Track(humanEmbryo); // Mark this embryo as a clone by adding its hash to a static list stored in EmbryoTracker, to be checked later by harmony patches within HumanEmbryo
			//Log.Message("Embryo added to EmbryoTracker, calling TryPopulateGenes...");
			humanEmbryo.TryPopulateGenes();

			return humanEmbryo;
		}
	}
}
