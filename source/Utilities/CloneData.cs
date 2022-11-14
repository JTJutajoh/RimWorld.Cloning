using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace Dark.Cloning
{
    /// <summary>
    /// Data class that stores all important data about clones
    /// </summary>
    public class CloneData : IExposable
    {
        public CustomXenotype customXenotype;
        public XenotypeDef xenotype;
        public bool UniqueXenotype => customXenotype != null;

        public List<GeneDef> xenogenes => UniqueXenotype ? customXenotype.genes : xenotype.genes;

        public Pawn donorPawn;
        public Gender? fixedGender;
        public HeadTypeDef headType;
        public Color? skinColorOverride;
        public FurDef furDef;
        public HairDef hairDef;
        public BeardDef beardDef;
        public BodyTypeDef bodyType;

        public CloneData() { }
        public CloneData(Pawn donorPawn, CustomXenotype customXenotype)
        {
            this.donorPawn = donorPawn;
            this.customXenotype = customXenotype;

            this.fixedGender = this.donorPawn?.gender;
            this.headType = this.donorPawn?.story?.headType;
            this.skinColorOverride = this.donorPawn?.story?.skinColorOverride;
            this.furDef = this.donorPawn?.story?.furDef;
            this.hairDef = this.donorPawn?.story?.hairDef;
            this.beardDef = this.donorPawn?.style?.beardDef;
            this.bodyType = this.donorPawn?.story?.bodyType;
        }
        public CloneData(Pawn donorPawn, XenotypeDef xenotypeDef)
        {
            this.donorPawn = donorPawn;
            this.xenotype = xenotypeDef;

            this.fixedGender = this.donorPawn?.gender;
            this.headType = this.donorPawn?.story?.headType;
            this.skinColorOverride = this.donorPawn?.story?.skinColorOverride;
            this.furDef = this.donorPawn?.story?.furDef;
            this.hairDef = this.donorPawn?.story?.hairDef;
            this.beardDef = this.donorPawn?.style?.beardDef;
            this.bodyType = this.donorPawn?.story?.bodyType;
        }
        public CloneData(Pawn donorPawn)
        {
            this.donorPawn = donorPawn;

            if (donorPawn.genes.UniqueXenotype)
            {
                //TODO: When unstable hits main, switch to this commented code
                //this.customXenotype = donorPawn.genes.CustomXenotype;
                this.customXenotype = new CustomXenotype();
                this.customXenotype.name = donorPawn.genes.xenotypeName;
                this.customXenotype.iconDef = donorPawn.genes.iconDef;
                this.customXenotype.genes = GeneUtils.GetGenesAsDefs(donorPawn.genes.GenesListForReading);
            }
            else
            {
                this.xenotype = donorPawn.genes.Xenotype;
            }

            this.fixedGender = this.donorPawn?.gender;
            this.headType = this.donorPawn?.story?.headType;
            this.skinColorOverride = this.donorPawn?.story?.skinColorOverride;
            this.furDef = this.donorPawn?.story?.furDef;
            this.hairDef = this.donorPawn?.story?.hairDef;
            this.beardDef = this.donorPawn?.style?.beardDef;
            this.bodyType = this.donorPawn?.story?.bodyType;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref this.customXenotype, "customXenotype");
            Scribe_Defs.Look(ref xenotype, "xenotype");
            
            Scribe_References.Look(ref this.donorPawn, "donorPawn");
            Scribe_Values.Look(ref this.fixedGender, "fixedGender");
            Scribe_Values.Look(ref this.skinColorOverride, "skinColorOverride");
            Scribe_Defs.Look(ref this.headType, "headType");
            Scribe_Defs.Look(ref this.furDef, "furDef");
            Scribe_Defs.Look(ref this.hairDef, "hairDef");
            Scribe_Defs.Look(ref this.beardDef, "beardDef");
            Scribe_Defs.Look(ref this.bodyType, "bodyType");
        }
    }
}
