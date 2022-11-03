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
        public Thing implantTarget;

        private static readonly Texture2D CancelImplantIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        public string sourceLabel = null;
        public Name sourceName = null;
        public PawnKindDef kindDef = null;

        public BackstoryDef backStoryChild;
        public BackstoryDef backStoryAdult;

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
            if (implantTarget == null)
            {

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
                    implantTarget = null;
                };
                command_Action3.activateSound = SoundDefOf.Designate_Cancel;
                yield return command_Action3;
            }
        }

        private void EnsureImplantTargetValid()
        {
            if (implantTarget == null)
            {
                return;
            }
            if (implantTarget is Building_CloneVat building_CloneVat && ( building_CloneVat.Destroyed || !ImplantVatValid(cancel: false) ))
            {
                implantTarget = null;
            }
        }

        private bool ImplantVatValid(bool cancel)
        {
            if (implantTarget is Building_CloneVat building_CloneVat)
            {
                if (cancel)
                {
                    building_CloneVat.selectedBrainScan = null;
                    implantTarget = null;
                    return false;
                }
                return building_CloneVat.selectedBrainScan == this;
            }
            return false;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions_NonPawn(Thing selectedThing)
        {
            Building_CloneVat cloneVat;
            if (( cloneVat = selectedThing as Building_CloneVat ) == null)
            {
                yield break;
            }
            if (cloneVat.SelectedPawn != null)
            {
                yield return new FloatMenuOption("CannotInsertBrainScan".Translate() + ": " + "Occupied".Translate(), null);
            }
            else if (cloneVat.selectedBrainScan == null)
            {
                yield return new FloatMenuOption("InstallBrainScan".Translate(), delegate
                {
                    cloneVat.SelectBrainScan(this);
                }, this, Color.white);
            }
            else
            {
                yield return new FloatMenuOption("CannotInsertBrainScan".Translate() + ": " + "BrainScanAlreadyInside".Translate(), null);
            }
        }

        public void ScanPawn(Pawn pawn)
        {
            sourceLabel = pawn?.Name?.ToStringFull ?? null;
            sourceName = pawn?.Name ?? null;
            kindDef = pawn?.kindDef ?? null;

            if (pawn.story != null)
            {
                backStoryChild = pawn.story.Childhood;
                backStoryAdult = pawn.story.Adulthood;
            }

            // Backup skills
            skills = new List<SkillRecordBackup>();
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                skills.Add(new SkillRecordBackup(skill));
            }

            // Backup traits
            traits = new List<TraitBackup>();
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (trait.sourceGene != null) continue; // Don't add genetic traits
                traits.Add(new TraitBackup(trait));
            }

            // Backup social relations
            //TODO: Backup social relations in the brain scan
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref sourceLabel, "sourceName");
            Scribe_Defs.Look(ref kindDef, "kindDef");
            // QEE does this by getting the identifier and checking for it. not sure why, might need to do the same instead
            Scribe_Defs.Look(ref backStoryChild, "backstoryChild");
            Scribe_Defs.Look(ref backStoryAdult, "backstoryAdult");

            Scribe_Collections.Look(ref traits, "traits");
            Scribe_Collections.Look(ref skills, "skills");

            Scribe_References.Look(ref implantTarget, "implantTarget");
        }

        public override string GetInspectString()
        {
            string result = "";

            result = $"Brain scan of {sourceLabel}"; //TODO: Key this inspect string

            return result;
        }
    }

    // Implemented similarly to Questionable Ethics
    public class SkillRecordBackup : IExposable
    {
        public SkillDef def;
        public Passion passion;
        public int level;
        public float xpSinceLastLevel;

        public SkillRecordBackup(SkillRecord record)
        {
            def = record.def;
            passion = record.passion;
            level = record.GetLevel(false);
            xpSinceLastLevel = record.xpSinceLastLevel;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref passion, "passion");
            Scribe_Values.Look(ref level, "level");
            Scribe_Values.Look(ref xpSinceLastLevel, "xpSinceLastLevel");
        }
    }

    public class TraitBackup : IExposable
    {
        public TraitDef def;
        public int degree;


        public TraitBackup(Trait trait)
        {
            def = trait.def;
            degree = trait.Degree;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref degree, "degree");
        }
    }
}