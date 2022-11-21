using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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

        public Pawn ContainedPawn => this.innerContainer.Count <= 0 ? (Pawn)null : (Pawn)this.innerContainer[0];

        [Unsaved(false)]
        private Effecter progressBar;

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

        [Unsaved(false)]
        private Texture2D cachedInsertPawnTex;
        public Texture2D InsertPawnTex
        {
            get
            {
                if ((UnityEngine.Object)this.cachedInsertPawnTex == (UnityEngine.Object)null)
                    this.cachedInsertPawnTex = ContentFinder<Texture2D>.Get("UI/Gizmos/InsertPawn");
                return this.cachedInsertPawnTex;
            }
        }

        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        private static readonly int scanningTicks = 3000;
        private int ticksRemaining;

        public override void PostPostMake()
        {
            if (!ModLister.CheckBiotech("brain scanner"))
                this.Destroy(DestroyMode.Vanish);
            else
                base.PostPostMake();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            //this.sustainerWorking = (Sustainer)null;
            if (this.progressBar != null)
            {
                this.progressBar.Cleanup();
                this.progressBar = (Effecter)null;
            }
            base.DeSpawn(mode);
        }

        public override void Tick()
        {
            base.Tick();
            this.innerContainer.ThingOwnerTick();
            if (this.IsHashIntervalTick(250))
                this.PowerTraderComp.PowerOutput = ( this.Working ? -this.PowerComp.Props.PowerConsumption : -this.PowerComp.Props.idlePowerDraw );
            if (this.Working && this.PowerTraderComp.PowerOn)
            {
                this.TickEffects();
                if (this.PowerOn)
                    --this.ticksRemaining;
                if (this.ticksRemaining <= 0)
                    this.Finish();
            }
            else
            {
                if (this.progressBar == null)
                    return;
                this.progressBar.Cleanup();
                this.progressBar = (Effecter)null;
            }
        }

        private void TickEffects()
        {
            //if (this.sustainerWorking == null || this.sustainerWorking.Ended)
            //    this.sustainerWorking = SoundDefOf.GeneExtractor_Working.TrySpawnSustainer(SoundInfo.InMap((TargetInfo)(Thing)this, MaintenanceType.PerTick));
            //else
            //    this.sustainerWorking.Maintain();
            if (this.progressBar == null)
                this.progressBar = EffecterDefOf.ProgressBarAlwaysVisible.Spawn();
            this.progressBar.EffectTick(new TargetInfo(this.Position + IntVec3.North.RotatedBy(this.Rotation), this.Map), TargetInfo.Invalid);
            MoteProgressBar mote = ( (SubEffecter_ProgressBar)this.progressBar.children[0] ).mote;
            if (mote == null)
                return;
            mote.progress = 1f - Mathf.Clamp01((float)this.ticksRemaining / scanningTicks);
            mote.offsetZ = this.Rotation == Rot4.North ? 0.5f : -0.5f;
        }

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

            int num = pawn.DeSpawnOrDeselect() ? 1 : 0;
            if (this.innerContainer.TryAddOrTransfer((Thing)pawn))
            {
                this.startTick = Find.TickManager.TicksGame;
                this.ticksRemaining = scanningTicks;
            }
            if (num == 0)
                return;
            Find.Selector.Select((object)pawn, false, false);
        }

        private void Reset()
        {
            startTick = -1;
            innerContainer.TryDropAll(def.hasInteractionCell ? InteractionCell : base.Position, base.Map, ThingPlaceMode.Near);
        }

        private void Cancel()
        {
            this.startTick = -1;
            this.selectedPawn = null;
            this.innerContainer.TryDropAll(this.def.hasInteractionCell ? this.InteractionCell : this.Position, this.Map, ThingPlaceMode.Near);
        }

        private void Finish()
        {
            this.startTick = -1;
            this.selectedPawn = null;
            //this.sustainerWorking = null;
            if (this.ContainedPawn == null)
                return;
            ProduceBrainScan();
        }

        private void ProduceBrainScan()
        {
            Pawn pawn = this.ContainedPawn;
            IntVec3 dropPos = this.def.hasInteractionCell ? this.InteractionCell : this.Position;

            var brainScan = (BrainScan)ThingMaker.MakeThing(CloneDefOf.BrainScan);
            brainScan.Scan(pawn);
            this.innerContainer.TryDropAll(dropPos, this.Map, ThingPlaceMode.Near);
            //TODO: Add a thought about having had your brain scanned
            GenPlace.TryPlaceThing((Thing)brainScan, dropPos, this.Map, ThingPlaceMode.Near);
            Messages.Message(
                "Cloning_BrainScanComplete".Translate(pawn.Named("PAWN")),
                new LookTargets(new TargetInfo[2]
                    {
                        (TargetInfo) (Thing) pawn,
                        (TargetInfo) (Thing) brainScan
                    }
                ),
                MessageTypeDefOf.PositiveEvent
            );
        }

        public override void Draw()
        {
            base.Draw();
            if (!this.Working || this.selectedPawn == null || !this.innerContainer.Contains((Thing)this.selectedPawn))
                return;
            this.selectedPawn.Drawer.renderer.RenderPawnAt(this.DrawPos + this.PawnDrawOffset, neverAimWeapon: true);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
                yield return floatMenuOption;

            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            AcceptanceReport acceptanceReport = CanAcceptPawn(selPawn);
            if (acceptanceReport.Accepted)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                {
                    SelectPawn(selPawn);
                }), selPawn, this);
            }
            else if (base.selectedPawn == selPawn && !selPawn.IsPrisonerOfColony)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                {
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.EnterBuilding, this), JobTag.Misc);
                }), selPawn, this);
            }
            else if (!acceptanceReport.Reason.NullOrEmpty())
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + acceptanceReport.Reason.CapitalizeFirst(), null);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            if (base.Working || this.innerContainer.Count > 0)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "CommandCancelExtraction".Translate();
                command_Action.defaultDesc = "CommandCancelExtractionDesc".Translate();
                command_Action.icon = CancelIcon;
                command_Action.action = delegate
                {
                    Cancel();
                };
                command_Action.activateSound = SoundDefOf.Designate_Cancel;
                yield return command_Action;
            }
            if (!base.Working)
            {
                if (selectedPawn != null)
                {
                    Command_Action command_Action3 = new Command_Action();
                    command_Action3.defaultLabel = "CommandCancelLoad".Translate();
                    command_Action3.defaultDesc = "CommandCancelLoadDesc".Translate();
                    command_Action3.icon = CancelIcon;
                    command_Action3.activateSound = SoundDefOf.Designate_Cancel;
                    command_Action3.action = delegate
                    {
                        innerContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Near);
                        if (selectedPawn.CurJobDef == JobDefOf.EnterBuilding)
                        {
                            selectedPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                        selectedPawn = null;
                        startTick = -1;
                        //sustainerWorking = null;
                    };
                    yield return command_Action3;
                    yield break;
                }

                Command_Action command_Action4 = new Command_Action();
                command_Action4.defaultLabel = "InsertPerson".Translate() + "...";
                command_Action4.defaultDesc = "InsertPersonBrainScannerDesc".Translate();
                command_Action4.icon = InsertPawnTex;
                command_Action4.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (Pawn item in base.Map.mapPawns.AllPawnsSpawned)
                    {
                        Pawn pawn = item;
                        AcceptanceReport acceptanceReport = CanAcceptPawn(item);
                        if (!acceptanceReport.Accepted)
                        {
                            if (!acceptanceReport.Reason.NullOrEmpty())
                            {
                                list.Add(new FloatMenuOption(item.LabelShortCap + ": " + acceptanceReport.Reason, null, pawn, Color.white));
                            }
                        }
                        else
                        {
                            list.Add(new FloatMenuOption(item.LabelShortCap + ", " + pawn.genes.XenotypeLabelCap, delegate
                            {
                                SelectPawn(pawn);
                            }, pawn, Color.white));
                        }
                    }
                    if (!list.Any())
                    {
                        list.Add(new FloatMenuOption("NoExtractablePawns".Translate(), null));
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                };
                if (!PowerOn)
                {
                    command_Action4.Disable("NoPower".Translate().CapitalizeFirst());
                }
                yield return command_Action4;
            }
            // Debug gizmo(s)
            else if (DebugSettings.ShowDevGizmos && base.Working)
            {
                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "DEV: Finish extraction";
                command_Action2.action = delegate
                {
                    Finish();
                };
                yield return command_Action2;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder result = new StringBuilder(base.GetInspectString());

            if (this.selectedPawn != null && this.innerContainer.Count == 0)
            {
                result.AppendInNewLine("WaitingForPawn".Translate(this.selectedPawn.Named("PAWN")).Resolve());
            }
            else if (this.Working && this.ContainedPawn != null)
            {
                result.AppendInNewLine("Cloning_ScanningBrainOf".Translate(this.ContainedPawn.Named("PAWN")).Resolve());
            }

            return result.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining");

        }
    }
}