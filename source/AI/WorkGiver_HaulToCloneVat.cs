using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace Dark.Cloning
{
    class WorkGiver_HaulToCloneVat : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(CloneDefOf.CloneStorageVat);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!ModLister.CheckBiotech("Clone vat"))
			{
				return false;
			}
			if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
			{
				return false;
			}
			if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
			{
				return false;
			}
			if (t.IsBurning())
			{
				return false;
			}
			if (!( t is Building_CloneStorageVat building_CloneVat ))
			{
				return false;
			}
			
			if (building_CloneVat.selectedBrainScan != null && !building_CloneVat.innerContainer.Contains(building_CloneVat.selectedBrainScan))
			{
				return CanHaulSelectedThing(pawn, building_CloneVat.selectedBrainScan);
			}
			return false;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!( t is Building_CloneStorageVat building_CloneVat ))
			{
				return null;
			}
			if (building_CloneVat.selectedBrainScan != null && !building_CloneVat.innerContainer.Contains(building_CloneVat.selectedBrainScan) && CanHaulSelectedThing(pawn, building_CloneVat.selectedBrainScan))
			{
				Job job2 = HaulAIUtility.HaulToContainerJob(pawn, building_CloneVat.selectedBrainScan, t);
				job2.count = 1;
				
				return job2;
			}
			return null;
		}

		private bool CanHaulSelectedThing(Pawn pawn, Thing selectedThing)
		{
			if (!selectedThing.Spawned || selectedThing.Map != pawn.Map)
			{
				return false;
			}
			if (selectedThing.IsForbidden(pawn) || !pawn.CanReserveAndReach(selectedThing, PathEndMode.OnCell, Danger.Deadly, 1, 1))
			{
				return false;
			}
			return true;
		}
	}
}
