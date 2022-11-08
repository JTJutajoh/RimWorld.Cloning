using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
    public class HediffComp_Pregnant_Clone : HediffComp
    {
        HediffCompProperties_PregnantClone Props => (HediffCompProperties_PregnantClone)this.props;

        public CloneData cloneData;

        public bool IsClone()
        {
            if (cloneData == null) return false;
            else return true;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref cloneData, "cloneData");
        }
    }

    public class HediffCompProperties_PregnantClone : HediffCompProperties
    {

        public HediffCompProperties_PregnantClone()
        {
            this.compClass = typeof(Comp_CloneEmbryo);
        }
        public HediffCompProperties_PregnantClone(Type compClass)
        {
            this.compClass = compClass;
        }
    }
}
