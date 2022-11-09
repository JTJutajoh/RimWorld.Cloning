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
        public GeneSet forcedXenogenes;
        public Pawn donorPawn;
        public Gender? fixedGender;
        public HeadTypeDef headType;
        public Color? skinColorOverride;
        public FurDef furDef;
        public HairDef hairDef;
        public BeardDef beardDef;

        public CloneData(Pawn donorPawn, GeneSet forcedXenogenes)
        {
            this.donorPawn = donorPawn;
            this.forcedXenogenes = forcedXenogenes;

            this.fixedGender = this.donorPawn?.gender;
            this.headType = this.donorPawn?.story?.headType;
            this.skinColorOverride = this.donorPawn?.story?.skinColorOverride;
            this.furDef = this.donorPawn?.story?.furDef;
            this.hairDef = this.donorPawn?.story?.hairDef;
            this.beardDef = this.donorPawn?.style?.beardDef;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref forcedXenogenes, "forcedXenogenes");
            Scribe_References.Look(ref donorPawn, "donorPawn");
            Scribe_Values.Look(ref fixedGender, "fixedGender");
        }
    }
}
