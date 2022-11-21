using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Dark.Cloning
{
    public class Comp_BrainScanContainer : ThingComp
    {
        public CompProperties_BrainScanContainer Props => (CompProperties_BrainScanContainer)this.props;


    }

    public class CompProperties_BrainScanContainer : CompProperties
    {
        public int maxCapacity;

        public CompProperties_BrainScanContainer()
        {
            this.compClass = typeof(Comp_BrainScanContainer);
        }
        public CompProperties_BrainScanContainer(Type compClass)
        {
            this.compClass = compClass;
        }
    }
}
