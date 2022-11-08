using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Dark.Cloning
{
    /// <summary>
    /// Data class that stores all important data about clones
    /// </summary>
    public class CloneData : IExposable
    {
        public GeneSet forcedXenogenes;

        public CloneData() { }
        public CloneData(GeneSet forcedXenogenes)
        {
            Log.Message($"Creating new clone data. {forcedXenogenes.GenesListForReading.Count} genes in the GeneSet");
            this.forcedXenogenes = forcedXenogenes;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref forcedXenogenes, "forcedXenogenes");
        }
    }
}
