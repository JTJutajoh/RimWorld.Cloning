using Verse;

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