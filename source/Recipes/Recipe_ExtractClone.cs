using System.Collections.Generic;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
	/*
	//TODO: Remove clone surgery
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
			if (pawn.ageTracker.AgeBiologicalYears < this.recipe.minAllowedAge)
			{
				return "CannotMustBeAge".Translate(this.recipe.minAllowedAge);
			}
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


		/// <summary>
		/// Similar to the version in Recipe_ExtractOvum but calls custom methods for clone behavior,
		/// as well as skipping the ovum stage altogether.
		/// </summary>
		protected override void OnSurgerySuccess(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			Thing embryo = CloneUtils.ProduceCloneEmbryo(pawn);

			CloneUtils.StartXenogermReplicatingHediff(pawn);

			if (!GenPlace.TryPlaceThing(embryo, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, (IntVec3 x) => x.InBounds(pawn.Map) && x.Standable(pawn.Map) && !x.Fogged(pawn.Map), default(Rot4)))
			{
				Log.Error("Could not drop embryo near " + pawn.Position);
			}
		}
	}*/
}
