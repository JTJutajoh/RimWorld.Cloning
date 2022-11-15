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
	/// <summary>
	/// Used by mech cloning serum.<br />
	/// Custom version of vanilla's comp, this one allows ONLY corpses to be targeted, regardless of desiccation.
	/// </summary>
    class CompTargetable_SingleHumanlikeCorpse : CompTargetable
    {
		protected override bool PlayerChoosesTarget => true;

		protected override TargetingParameters GetTargetingParameters()
		{
			return new TargetingParameters
			{
				canTargetPawns = false,
				canTargetBuildings = false,
				canTargetItems = true,
				mapObjectTargetsMustBeAutoAttackable = false,
				validator = (TargetInfo x) => x.Thing is Corpse && x.Thing.HasThingCategory(ThingCategoryDefOf.CorpsesHumanlike) && BaseTargetValidator(x.Thing)
			};
		}

		public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
		{
			yield return targetChosenByPlayer;
		}
	}
}
