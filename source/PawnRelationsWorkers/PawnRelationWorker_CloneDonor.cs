using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
    // Implied relation
    class PawnRelationWorker_CloneDonor : PawnRelationWorker
    {
        public override bool InRelation(Pawn me, Pawn other)
        {
            return me != other && other.relations.GetDirectRelation(CloneDefOf.CloneChild, me) != null;
        }
    }
}
