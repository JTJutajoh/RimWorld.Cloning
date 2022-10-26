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

        static CloneDefs()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CloneDefs));
        }
    }
}
