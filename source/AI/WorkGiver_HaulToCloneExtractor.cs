using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace Dark.Cloning
{
    class WorkGiver_HaulToCloneExtractor : WorkGiver_Scanner
    {
		public override ThingRequest PotentialWorkThingRequest
		{
			get
			{
				return ThingRequest.ForDef(CloneDefOf.CloneExtractor);
			}
		}
		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			return !ModsConfig.BiotechActive;
		}
		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_CloneExtractor building_CloneExtractor))
				return false;

			if (building_CloneExtractor.ArchitesRequiredNow <= 0)
				return false;

			if (this.FindArchiteCapsule(pawn) == null)
			{
				JobFailReason.Is("NoIngredient".Translate(ThingDefOf.ArchiteCapsule), null);
				return false;
			}
			return true;
		}
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_CloneExtractor building_CloneExtractor))
				return null;

			if (building_CloneExtractor.ArchitesRequiredNow > 0)
			{
				Thing thing = this.FindArchiteCapsule(pawn);
				if (thing != null)
				{
					Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer, thing, t);
					job.count = Mathf.Min(building_CloneExtractor.ArchitesRequiredNow, thing.stackCount);
					return job;
				}
			}
			return null;
			//return JobMaker.MakeJob(JobDefOf.CreateXenogerm, t, 1200, true);
		}
		private Thing FindArchiteCapsule(Pawn pawn)
		{
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.ArchiteCapsule), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), 9999f, (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, false), null, 0, -1, false, RegionType.Set_Passable, false);
		}
	}
}
