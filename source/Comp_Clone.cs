using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Dark.Cloning
{
    class Comp_Clone : ThingComp
    {
        public CompProperties_Clone Props => (CompProperties_Clone)this.props;

        private bool _isClone = false;
        public bool IsClone { get => _isClone; set => _isClone = value; }
    }

    public class CompProperties_Clone : CompProperties
    {
        public CompProperties_Clone()
        {
            this.compClass = typeof(Comp_Clone);
        }

        public CompProperties_Clone(Type compClass)
        {
            this.compClass = compClass;
        }
    }
}