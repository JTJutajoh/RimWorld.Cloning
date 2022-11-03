using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Dark.Cloning
{
    public class BrainScan : ThingWithComps
    {
        public string sourceName = null;
        public PawnKindDef kindDef = null;

        public BackstoryDef backStoryChild;
        public BackstoryDef backStoryAdult;
        //Skills

        //Animal stuff

        //Hediffs?


        public void ScanPawn(Pawn pawn)
        {
            sourceName = pawn?.Name?.ToStringFull ?? null;
            kindDef = pawn?.kindDef ?? null;

            if (pawn.story != null)
            {
                backStoryChild = pawn.story.Childhood;
                backStoryAdult = pawn.story.Adulthood;
            }

            // Backup skills
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref sourceName, "sourceName");
            Scribe_Defs.Look(ref kindDef, "kindDef");
            // QEE does this by getting the identifier and checking for it. not sure why, might need to do the same instead
            Scribe_Defs.Look(ref backStoryChild, "backstoryChild");
            Scribe_Defs.Look(ref backStoryAdult, "backstoryAdult");

            // Everything else
        }

        public override string GetInspectString()
        {
            string result = "";

            result = $"Brain scan of {sourceName}"; //TODO: Key this inspect string

            return result;
        }
    }
}