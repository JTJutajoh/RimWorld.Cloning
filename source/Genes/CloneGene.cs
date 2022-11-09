using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
    public class CloneGene : Gene
    {
        public CloneData cloneData;

        public override bool Active => true; // Clone gene is always active, cannot be overridden. A clone is always a clone

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref cloneData, "cloneData");
        }
    }
}