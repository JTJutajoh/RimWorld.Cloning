using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace Dark.Cloning
{
    public class CompTargetEffect_ScanCorpse : CompTargetEffect
    {
        public CompProperties_TargetEffectScanCorpse Props => (CompProperties_TargetEffectScanCorpse)props;

        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (user.IsColonistPlayerControlled && user.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                Job job = JobMaker.MakeJob(CloneDefs.ScanCorpse, target, parent);
                job.count = 1;
                user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }
    }

    public class CompProperties_TargetEffectScanCorpse : CompProperties
    {
        public ThingDef moteDef;

        public CompProperties_TargetEffectScanCorpse()
        {
            compClass = typeof(CompTargetEffect_ScanCorpse);
        }
    }
}
