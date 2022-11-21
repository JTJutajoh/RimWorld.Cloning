using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dark.Cloning
{
    public class Building_BrainScanner : Building_Enterable, IThingHolderWithDrawnPawn, IThingHolder
    {
        public float HeldPawnDrawPos_Y => this.DrawPos.y + 0.04054054f;
        public float HeldPawnBodyAngle
        {
            get
            {
                Rot4 rot4 = this.Rotation;
                rot4 = rot4.Opposite;
                return rot4.AsAngle;
            }
        }
        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;
        public override Vector3 PawnDrawOffset => Vector3.zero;// IntVec3.West.RotatedBy(this.Rotation).ToVector3() / (float)this.def.size.x;

        [Unsaved(false)]
        private CompPowerTrader cachedPowerComp;
        private CompPowerTrader PowerTraderComp
        {
            get
            {
                if (this.cachedPowerComp == null)
                    this.cachedPowerComp = this.TryGetComp<CompPowerTrader>();
                return this.cachedPowerComp;
            }
        }
        public bool PowerOn => this.PowerTraderComp.PowerOn;

        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony)
                return (AcceptanceReport)false;
            if (this.selectedPawn != null && this.selectedPawn != pawn)
                return (AcceptanceReport)false;
            if (!pawn.RaceProps.Humanlike || pawn.IsQuestLodger())
                return (AcceptanceReport)false;
            if (!this.PowerOn)
                return (AcceptanceReport)"NoPower".Translate().CapitalizeFirst();
            if (this.innerContainer.Count > 0)
                return (AcceptanceReport)"Occupied".Translate();
            //TODO: Add a check for the headache hediff like the subcore scanners
            return (AcceptanceReport)true;
        }

        public override void TryAcceptPawn(Pawn pawn)
        {
            if (!(bool)this.CanAcceptPawn(pawn))
                return;
            if (this.selectedPawn == null) return;
            this.selectedPawn = pawn;
            throw new NotImplementedException();
        }
    }
}