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
    class WorkGiver_CarryToCloneVat : WorkGiver_CarryToBuilding
	{
		public override ThingRequest ThingRequest => ThingRequest.ForDef(CloneDefOf.CloneVat);

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			return !ModsConfig.BiotechActive;
		}
	}
}
