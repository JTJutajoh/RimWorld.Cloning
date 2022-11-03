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
        public static JobDef ScanCorpse;
        public static ThingDef CloneExtractor;
        public static ThingDef CloneVat;
        public static ThingDef BrainScan;

        static CloneDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CloneDefOf));
        }
    }
}
