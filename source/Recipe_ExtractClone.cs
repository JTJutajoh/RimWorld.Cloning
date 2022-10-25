using System.Collections.Generic;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
	/// <summary>
	/// Modified version of vanilla Recipe_ExtractOvum class, removing restrictions and changing what Thing drops on success.
	/// </summary>
    public class Recipe_ExtractClone : Recipe_AddHediff
    {
		int sicknessDuration = 60000 * 7; // 7 days

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

		public override bool CompletableEver(Pawn surgeryTarget)
		{
			return base.IsValidNow(surgeryTarget, null, true);
		}

		protected override void OnSurgerySuccess(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
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

			hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = sicknessDuration; //TODO: Move this to settings or something.
        }

		/// <summary>
		/// Modified version of vanilla's ProduceEmbryo, gets the custom Comp_Clone from the created embryo and sets it to be a clone BEFORE calling TryPopulateGenes
		/// </summary>
		//TODO: Roll ProduceCloneEmbryo into a transpiler instead?
		private Thing ProduceCloneEmbryo(Pawn father, HumanOvum ovum)
        {
			//TODO: See if we can just skip the ovum altogether.
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

			// Actually set the new embryo to be marked as a clone so the patch can pick it up and intercept how its genes are chosen.
			Comp_Clone cloneComp = humanEmbryo.TryGetComp<Comp_Clone>();
			if (cloneComp == null) Log.Error("Failed to find Comp_Clone on embryo.");
			cloneComp.IsClone = true;

			humanEmbryo.TryPopulateGenes();
			return humanEmbryo;
		}

		/*private Thing FertilizeOvumWithSelf(HumanOvum ovum, Pawn parent)
		{
			Log.Message("Fertilizing ovum with first parent.");
			//HumanOvum ovum = (HumanOvum)thing;
			CompHasPawnSources sources = ovum.TryGetComp<CompHasPawnSources>();
			//sources.AddSource(parent);
			//sources.pawnSources.Add(parent);
			//Find.WorldPawns.AddPawnSource(parent, sources);
			//Log.Message($"Parent was {parent.ToString()}. Added {sources.pawnSources[1].ToString()}");
			return ProduceCloneEmbryo(parent, ovum);
		}*/
	}
}
