using System;
using Verse;

namespace Dark.Cloning
{
    public class HediffComp_Pregnant_Clone : HediffComp
    {
        HediffCompProperties_PregnantClone Props => (HediffCompProperties_PregnantClone)this.props;

        public CloneData cloneData;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Deep.Look(ref cloneData, "cloneData");
        }

        public override string ToString()
        {
            return nameof(HediffComp_Pregnant_Clone);
        }

        public override string CompDebugString()
        {
            return $"CloneComp {(cloneData != null ? "has clone data" : "no clone data")}";
        }
    }

    public class HediffCompProperties_PregnantClone : HediffCompProperties
    {

        public HediffCompProperties_PregnantClone()
        {
            this.compClass = typeof(HediffComp_Pregnant_Clone);
        }
        public HediffCompProperties_PregnantClone(Type compClass)
        {
            this.compClass = compClass;
        }
    }
}
