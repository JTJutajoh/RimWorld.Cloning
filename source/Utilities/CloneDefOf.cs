using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Dark.Cloning
{
    [DefOf]
    public static class CloneDefOf
    {
        public static GeneDef Clone;

        public static PawnRelationDef CloneDonor;
        public static PawnRelationDef CloneChild;

        public static ThingDef CloneExtractor;

        //public static ThingDef CloneStorageVat;

        public static ThingDef BrainScanner;
        public static ThingDef BrainScanProcessor;
        public static ThingDef BrainScanBank;
        public static ThingDef BrainScan;
        public static StatDef BrainScanEfficiencyIncrease;

        public static JobDef ScanCorpse;

        static CloneDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CloneDefOf));
        }
    }
}
