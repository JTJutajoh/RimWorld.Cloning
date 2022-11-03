using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Dark.Cloning
{
    public static class BrainUtil
    {
        public static void ScanPawn(Pawn pawn, BrainScan brainScan)
        {
            brainScan.sourceLabel = pawn?.Name?.ToStringFull ?? null;
            brainScan.sourceName = pawn?.Name ?? null;
            brainScan.kindDef = pawn?.kindDef ?? null;

            if (pawn.story != null)
            {
                brainScan.backStoryChild = pawn.story.Childhood;
                brainScan.backStoryAdult = pawn.story.Adulthood;
            }

            if (pawn.Ideo != null)
            {
                brainScan.scannedIdeology = pawn.Ideo;
            }

            // Backup skills
            brainScan.skills = new List<SkillRecordBackup>();
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                brainScan.skills.Add(new SkillRecordBackup(skill));
            }

            // Backup traits
            brainScan.traits = new List<TraitBackup>();
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (trait.sourceGene != null) continue; // Don't add genetic traits
                brainScan.traits.Add(new TraitBackup(trait));
            }

            // Backup social relations
            //TODO: Backup social relations in the brain scan
        }

        public static void ApplyBrainScanToPawn(Pawn pawn, BrainScan brainScan)
        {
            // Name
            pawn.Name = brainScan.sourceName;

            if (brainScan.scannedIdeology != null)
            {
                pawn.ideo.SetIdeo(brainScan.scannedIdeology);
            }

            // Backstories
            if (pawn.story != null)
            {
                pawn.story.Childhood = brainScan.backStoryChild; //QEE Has this commented out for some reason
                pawn.story.Adulthood = brainScan.backStoryAdult;
            }

            // Traits
            // First clear the old traits
            List<Trait> tmpList = new List<Trait>(pawn.story.traits.allTraits);
            foreach (Trait trait in tmpList)
            {
                // Use RemoveTrait just incase there are any notifies or harmony patches or anything looking for trait removal
                pawn.story.traits.RemoveTrait(trait);
            }
            // Then add the new ones
            foreach (TraitBackup trait in brainScan.traits)
            {
                pawn.story.traits.GainTrait(new Trait(trait.def, trait.degree));
            }

            // Skills
            for (int i = 0; i < brainScan.skills.Count; i++) // This will break if for some reason the skills in the game changed since the scan was made, such as if the player installed vanilla skills expanded. But that's their own fault
            {
                var scannedSkill = brainScan.skills[i];
                SkillRecord skill = pawn.skills.GetSkill(scannedSkill.def);
                skill.Level = scannedSkill.level; //SOMEDAY: Maybe an efficiency setting for brain scan skill copying?
                skill.passion = scannedSkill.passion;
                skill.xpSinceLastLevel = scannedSkill.xpSinceLastLevel;
                skill.Notify_SkillDisablesChanged();
            }

            // Social relations
            //TODO: Copy social relations
        }
    }

    public class SkillRecordBackup : IExposable
    {
        public SkillDef def;
        public Passion passion;
        public int level;
        public float xpSinceLastLevel;

        public SkillRecordBackup(SkillRecord record)
        {
            this.def = record.def;
            this.passion = record.passion;
            this.level = record.GetLevel(false);
            this.xpSinceLastLevel = record.xpSinceLastLevel;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref this.def, "def");
            Scribe_Values.Look(ref this.passion, "passion");
            Scribe_Values.Look(ref this.level, "level");
            Scribe_Values.Look(ref this.xpSinceLastLevel, "xpSinceLastLevel");
        }
    }

    public class TraitBackup : IExposable
    {
        public TraitDef def;
        public int degree;


        public TraitBackup(Trait trait)
        {
            this.def = trait.def;
            this.degree = trait.Degree;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref this.def, "def");
            Scribe_Values.Look(ref this.degree, "degree");
        }
    }
}
