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
    /*public class BrainScan : ThingWithComps
    {
        public Thing implantTarget;

        private static readonly Texture2D CancelImplantIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        public string sourceLabel = null;
        public Name sourceName = null;
        public PawnKindDef kindDef = null;

        public BackstoryDef backStoryChild;
        public BackstoryDef backStoryAdult;

        public Ideo scannedIdeology;

        public List<TraitBackup> traits;
        //Skills
        public List<SkillRecordBackup> skills;

        //Animal stuff

        //Hediffs?

        // Code mostly copied from HumanEmbryo in vanilla
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            EnsureImplantTargetValid();
            if (this.implantTarget == null)
            {
                //TODO: Add a gizmo to brain scans to implant them into clone vats?
            }
            else if (base.Spawned)
            {
                Command_Action command_Action3 = new Command_Action();
                command_Action3.defaultLabel = "ImplantBrainScanCancel".Translate();
                command_Action3.defaultDesc = "ImplantBrainScanCancel".Translate();
                command_Action3.icon = CancelImplantIcon;
                command_Action3.action = delegate
                {
                    ImplantVatValid(cancel: true);
                    this.implantTarget = null;
                };
                command_Action3.activateSound = SoundDefOf.Designate_Cancel;
                yield return command_Action3;
            }
            //TODO: Add a gizmo that shows a new menu that displays the scanned details?
        }
        // Code mostly copied from HumanEmbryo in vanilla
        private void EnsureImplantTargetValid()
        {
            if (this.implantTarget == null)
            {
                return;
            }
            if (this.implantTarget is Building_CloneStorageVat building_CloneVat && ( building_CloneVat.Destroyed || !ImplantVatValid(cancel: false) ))
            {
                this.implantTarget = null;
            }
        }
        // Code mostly copied from HumanEmbryo in vanilla
        private bool ImplantVatValid(bool cancel)
        {
            if (this.implantTarget is Building_CloneStorageVat building_CloneVat)
            {
                if (cancel)
                {
                    building_CloneVat.selectedBrainScan = null;
                    this.implantTarget = null;
                    return false;
                }
                return building_CloneVat.selectedBrainScan == this;
            }
            return false;
        }
        // Code mostly copied from HumanEmbryo in vanilla
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions_NonPawn(Thing selectedThing)
        {
            Building_CloneStorageVat cloneVat;
            if (( cloneVat = selectedThing as Building_CloneStorageVat ) == null)
            {
                yield break;
            }
            if (cloneVat.selectedBrainScan != null)
            {
                yield return new FloatMenuOption("CannotInsertBrainScan".Translate() + ": " + "BrainScanAlreadyInside".Translate(), null);
            }
            else
            {
                yield return new FloatMenuOption("InstallBrainScan".Translate(), delegate
                {
                    cloneVat.SelectBrainScan(this);
                }, this, Color.white);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref this.sourceLabel, "sourceName");
            Scribe_Defs.Look(ref this.kindDef, "kindDef");
            // QEE does this by getting the identifier and checking for it. not sure why, might need to do the same instead
            Scribe_Defs.Look(ref this.backStoryChild, "backstoryChild");
            Scribe_Defs.Look(ref this.backStoryAdult, "backstoryAdult");

            Scribe_Collections.Look(ref this.traits, "traits");
            Scribe_Collections.Look(ref this.skills, "skills");

            Scribe_References.Look(ref this.implantTarget, "implantTarget");
        }

        public override string GetInspectString()
        {
            string result = "";

            result = $"Brain scan of {this.sourceLabel}"; //TODO: Key this inspect string

            return result;
        }
    }*/

}