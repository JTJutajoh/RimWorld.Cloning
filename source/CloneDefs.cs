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
    public static class CloneDefs
    {
        public static GeneDef Clone;
        public static JobDef ScanCorpse;

        static CloneDefs()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CloneDefs));
        }
    }
}
