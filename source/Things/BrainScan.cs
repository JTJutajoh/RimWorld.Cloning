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


        }

        public override string GetInspectString()
        {
            StringBuilder result = new StringBuilder(); ;

            return result.ToString();
        }
    }

}