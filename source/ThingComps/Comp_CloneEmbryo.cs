using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Dark.Cloning
{
    public class Comp_CloneEmbryo : ThingComp
    {
        public CompProperties_CloneEmbryo Props => (CompProperties_CloneEmbryo)this.props;

        public CloneData cloneData;

        public bool IsClone()
        {
            if (cloneData == null) return false;
            else return true;
        }

        public override string CompInspectStringExtra()
        {
            return IsClone() ? "CloneEmbryoInspectString".Translate() : null;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref cloneData, "cloneData");
        }
    }

    public class CompProperties_CloneEmbryo : CompProperties
    {

        public CompProperties_CloneEmbryo()
        {
            this.compClass = typeof(Comp_CloneEmbryo);
        }
        public CompProperties_CloneEmbryo(Type compClass)
        {
            this.compClass = compClass;
        }
    }
}
