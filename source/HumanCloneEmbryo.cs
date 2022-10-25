using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace RimWorld
{
    public class HumanCloneEmbryo : HumanEmbryo
    {

        public void TryPopulateGenesForClone()
        {
            if (this.geneSet == null)
            {
                Log.Message("HumanCloneEmbryo: Copying genes for the clone embryo from the parent");
                this.geneSet = PregnancyUtility.GetInheritedGeneSet(this.Father, this.Father); //TODO: Actually copy them over manually
            }
        }
    }
}
