using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace Dark.Cloning
{
    public class BrainScan : ThingWithComps
    {
        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        public BrainData brainData = null;


        public void Scan(Pawn pawn)
        {
            if (brainData == null)
                brainData = new BrainData();
            brainData.ScanFrom(pawn);
        }

        public void Apply(Pawn pawn)
        {
            if (brainData == null)
                return;
            brainData.ApplyTo(pawn);
        }

        // Code mostly copied from HumanEmbryo in vanilla
        public override IEnumerable<Gizmo> GetGizmos()
        {
            yield return null;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions_NonPawn(Thing selectedThing)
        {
            yield return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref brainData, "brainData");
        }

        public override string GetInspectString()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("Cloning_BrainScanOf".Translate() + brainData.sourceLabel);

            return result.ToString();
        }
    }

}