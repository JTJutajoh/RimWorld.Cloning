using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Dark.Cloning
{
    public class BrainData : IExposable
    {
        public Pawn sourcePawn;

        public string sourceLabel;
        public Name sourceName;

        public PawnKindDef kindDef;

        public BackstoryDef backStoryChild;
        public BackstoryDef backStoryAdult;

        public Ideo ideology;

        public List<TraitBackup> traits;
        public List<SkillRecordBackup> skills;

        //Animal stuff

        //Hediffs?

        public BrainData()
        {
            ClearData();
        }

        public void ClearData()
        {
            sourcePawn = null;
            sourceLabel = null;
            sourceName = null;
            kindDef = null;
            backStoryAdult = null;
            backStoryChild = null;
            ideology = null;
            traits = null;
            skills = null;
        }

        public void ScanFrom(Pawn pawn)
        {
            this.sourceLabel = pawn?.Name?.ToStringFull ?? null;
            this.sourceName = pawn?.Name ?? null;
            this.kindDef = pawn?.kindDef ?? null;

            if (pawn.story != null)
            {
                this.backStoryChild = pawn.story.Childhood;
                this.backStoryAdult = pawn.story.Adulthood;
            }

            if (pawn.Ideo != null)
            {
                this.ideology = pawn.Ideo;
            }

            // Backup skills
            this.skills = new List<SkillRecordBackup>();
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                this.skills.Add(new SkillRecordBackup(skill));
            }

            // Backup traits
            this.traits = new List<TraitBackup>();
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (trait.sourceGene != null) continue; // Don't add genetic traits
                this.traits.Add(new TraitBackup(trait));
            }

            // Backup social relations
            //TODO: Backup social relations in the brain scan
        }

        public void ApplyTo(Pawn pawn)
        {
            // Name
            pawn.Name = this.sourceName;

            if (this.ideology != null)
            {
                pawn.ideo.SetIdeo(this.ideology);
            }

            // Backstories
            if (pawn.story != null)
            {
                pawn.story.Childhood = this.backStoryChild; //QEE Has this commented out for some reason
                pawn.story.Adulthood = this.backStoryAdult;
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
            foreach (TraitBackup trait in this.traits)
            {
                pawn.story.traits.GainTrait(new Trait(trait.def, trait.degree));
            }

            // Skills
            for (int i = 0; i < this.skills.Count; i++) // This will break if for some reason the skills in the game changed since the scan was made, such as if the player installed vanilla skills expanded. But that's their own fault
            {
                var scannedSkill = this.skills[i];
                SkillRecord skill = pawn.skills.GetSkill(scannedSkill.def);
                skill.Level = scannedSkill.level; //SOMEDAY: Maybe an efficiency setting for brain scan skill copying?
                skill.passion = scannedSkill.passion;
                skill.xpSinceLastLevel = scannedSkill.xpSinceLastLevel;
                skill.Notify_SkillDisablesChanged();
            }

            // Social relations
            //TODO: Copy social relations
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref sourcePawn, "sourcePawn");
            Scribe_Values.Look(ref sourceLabel, "sourceLabel");
            Scribe_Values.Look(ref sourceName, "sourceName");
            Scribe_Defs.Look(ref kindDef, "kindDef");
            Scribe_Defs.Look(ref backStoryChild, "backStoryChild");
            Scribe_Defs.Look(ref backStoryAdult, "backStoryAdult");
            Scribe_References.Look(ref ideology, "ideology");
            Scribe_Collections.Look(ref traits, "traits");
            Scribe_Collections.Look(ref skills, "skills");
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
