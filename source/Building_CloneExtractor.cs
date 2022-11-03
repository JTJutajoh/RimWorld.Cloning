using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;
using Verse.AI;

namespace Dark.Cloning
{
    /// <summary>
    /// Based on vanilla Building_CloneExtractor
    /// </summary>
    [StaticConstructorOnStartup]
    public class Building_CloneExtractor : Building_Enterable, IThingHolderWithDrawnPawn, IThingHolder
    {
        private int ticksRemaining;
        [Unsaved(false)]
        private CompPowerTrader cachedPowerComp;
        [Unsaved(false)]
        private Texture2D cachedInsertPawnTex;
        [Unsaved(false)]
        private Sustainer sustainerWorking;
        [Unsaved(false)]
        private Effecter progressBar;
        private const int TicksToExtract = 30000;
        private float WorkingPowerUsageFactor = 6f;
        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        public enum CloneExtractorModes
        {
            Embryo,
            Brain
        }
        [Unsaved(false)]
        private CloneExtractorModes currentMode = CloneExtractorModes.Embryo;

        private Pawn ContainedPawn => this.innerContainer.Count <= 0 ? (Pawn)null : (Pawn)this.innerContainer[0];

        public bool PowerOn => this.PowerTraderComp.PowerOn;

        private CompPowerTrader PowerTraderComp
        {
            get
            {
                if (this.cachedPowerComp == null)
                    this.cachedPowerComp = this.TryGetComp<CompPowerTrader>();
                return this.cachedPowerComp;
            }
        }

        public Texture2D InsertPawnTex
        {
            get
            {
                if ((UnityEngine.Object)this.cachedInsertPawnTex == (UnityEngine.Object)null)
                    this.cachedInsertPawnTex = ContentFinder<Texture2D>.Get("UI/Gizmos/InsertPawn");
                return this.cachedInsertPawnTex;
            }
        }

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

        public override void PostPostMake()
        {
            if (!ModLister.CheckBiotech("clone extractor"))
                this.Destroy(DestroyMode.Vanish);
            else
                base.PostPostMake();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.sustainerWorking = (Sustainer)null;
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
                this.PowerTraderComp.PowerOutput = -this.PowerComp.Props.PowerConsumption * ( this.Working ? this.WorkingPowerUsageFactor : 1f );
            if (this.Working && this.PowerTraderComp.PowerOn)
            {
                this.TickEffects();
                if (this.PowerOn)
                    --this.ticksRemaining;
                if (this.ticksRemaining > 0)
                    return;
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
            if (this.sustainerWorking == null || this.sustainerWorking.Ended)
                this.sustainerWorking = SoundDefOf.GeneExtractor_Working.TrySpawnSustainer(SoundInfo.InMap((TargetInfo)(Thing)this, MaintenanceType.PerTick));
            else
                this.sustainerWorking.Maintain();
            if (this.progressBar == null)
                this.progressBar = EffecterDefOf.ProgressBarAlwaysVisible.Spawn();
            this.progressBar.EffectTick(new TargetInfo(this.Position + IntVec3.North.RotatedBy(this.Rotation), this.Map), TargetInfo.Invalid);
            MoteProgressBar mote = ( (SubEffecter_ProgressBar)this.progressBar.children[0] ).mote;
            if (mote == null)
                return;
            mote.progress = 1f - Mathf.Clamp01((float)this.ticksRemaining / 30000f);
            mote.offsetZ = this.Rotation == Rot4.North ? 0.5f : -0.5f;
        }

        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony)
                return (AcceptanceReport)false;
            if (this.selectedPawn != null && this.selectedPawn != pawn)
                return (AcceptanceReport)false;
            if (!pawn.RaceProps.Humanlike || pawn.IsQuestLodger()) //SOMEDAY: Make the gene extractor accept non humanlike pawns?
                return (AcceptanceReport)false;
            if (!this.PowerOn)
                return (AcceptanceReport)"NoPower".Translate().CapitalizeFirst();
            if (this.innerContainer.Count > 0)
                return (AcceptanceReport)"Occupied".Translate();
            //if (pawn.genes == null || !pawn.genes.GenesListForReading.Any<Gene>())
            //    return (AcceptanceReport)"PawnHasNoGenes".Translate(pawn.Named("PAWN"));
            //if (!pawn.genes.GenesListForReading.Any<Gene>((Predicate<Gene>)( x => x.def.biostatArc == 0 )))
            //    return (AcceptanceReport)"PawnHasNoNonArchiteGenes".Translate(pawn.Named("PAWN"));
            return pawn.health.hediffSet.HasHediff(HediffDefOf.XenogerminationComa) ? (AcceptanceReport)"InXenogerminationComa".Translate() : (AcceptanceReport)true;
        }

        private void Cancel()
        {
            this.startTick = -1;
            this.selectedPawn = (Pawn)null;
            this.sustainerWorking = (Sustainer)null;
            if (this.ContainedPawn == null)
                return;
            this.innerContainer.TryDropAll(this.def.hasInteractionCell ? this.InteractionCell : this.Position, this.Map, ThingPlaceMode.Near);
        }

        private void Finish()
        {
            this.startTick = -1;
            this.selectedPawn = (Pawn)null;
            this.sustainerWorking = (Sustainer)null;
            if (this.ContainedPawn == null)
                return;
            Pawn containedPawn = this.ContainedPawn;
            IntVec3 intVec3 = this.def.hasInteractionCell ? this.InteractionCell : this.Position;
            switch (currentMode)
            {
                case CloneExtractorModes.Embryo:
                    HumanEmbryo embryo = (HumanEmbryo)CloneUtils.ProduceCloneEmbryo(this.ContainedPawn);
                    if (Settings.cloningCooldown) GeneUtility.ExtractXenogerm(containedPawn, Mathf.RoundToInt(60000f * Settings.CloneExtractorRegrowingDurationDaysRange.RandomInRange));
                    this.innerContainer.TryDropAll(intVec3, this.Map, ThingPlaceMode.Near);
                    if (!containedPawn.Dead && ( containedPawn.IsPrisonerOfColony || containedPawn.IsSlaveOfColony ))
                        containedPawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.XenogermHarvested_Prisoner); //SOMEDAY: Make a new type of thought instead of using the xenogerm harvested thought
                    GenPlace.TryPlaceThing((Thing)embryo, intVec3, this.Map, ThingPlaceMode.Near);
                    Messages.Message((string)( "Cloning_CloneExtractionComplete".Translate(containedPawn.Named("PAWN")) ),
                        new LookTargets(new TargetInfo[2]
                            {
                        (TargetInfo) (Thing) containedPawn,
                        (TargetInfo) (Thing) embryo
                            }
                        ), MessageTypeDefOf.PositiveEvent);
                    break;
                case CloneExtractorModes.Brain:
                    BrainScan brainScan = (BrainScan)ThingMaker.MakeThing(CloneDefs.BrainScan);
                    brainScan.ScanPawn(ContainedPawn);
                    this.innerContainer.TryDropAll(intVec3, this.Map, ThingPlaceMode.Near);
                    //TODO: Add a thought about having had your brain scanned
                    //if (!containedPawn.Dead && ( containedPawn.IsPrisonerOfColony || containedPawn.IsSlaveOfColony ))
                    //    containedPawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.XenogermHarvested_Prisoner); //SOMEDAY: Make a new type of thought instead of using the xenogerm harvested thought
                    GenPlace.TryPlaceThing((Thing)brainScan, intVec3, this.Map, ThingPlaceMode.Near);
                    Messages.Message((string)( "Cloning_BrainScanComplete".Translate(containedPawn.Named("PAWN")) ),
                        new LookTargets(new TargetInfo[2]
                            {
                        (TargetInfo) (Thing) containedPawn,
                        (TargetInfo) (Thing) brainScan
                            }
                        ), MessageTypeDefOf.PositiveEvent);
                    break;
                default:
                    Log.Error("Clone Extractor failed, invalid mode");
                    break;
            }
            
        }

        public override void TryAcceptPawn(Pawn pawn)
        {
            if (!(bool)this.CanAcceptPawn(pawn))
                return;
            this.selectedPawn = pawn;
            int num = pawn.DeSpawnOrDeselect() ? 1 : 0;
            if (this.innerContainer.TryAddOrTransfer((Thing)pawn))
            {
                this.startTick = Find.TickManager.TicksGame;
                this.ticksRemaining = 30000; //TODO: Redefine how long the clone extractor takes
            }
            if (num == 0)
                return;
            Find.Selector.Select((object)pawn, false, false);
        }

        protected override void SelectPawn(Pawn pawn)
        {
            if (currentMode == CloneExtractorModes.Embryo && pawn.health.hediffSet.HasHediff(HediffDefOf.XenogermReplicating))
                Find.WindowStack.Add((Window)Dialog_MessageBox.CreateConfirmation("ConfirmExtractXenogermWillKill".Translate(pawn.Named("PAWN")), (Action)( () => base.SelectPawn(pawn) )));
            else
                base.SelectPawn(pawn);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            AcceptanceReport acceptanceReport = CanAcceptPawn(selPawn);
            if (acceptanceReport.Accepted)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this) + " " + "Mode_Clone".Translate(), delegate
                {
                    currentMode = CloneExtractorModes.Embryo;
                    SelectPawn(selPawn);
                }), selPawn, this);
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this) + " " + "Mode_BrainScan".Translate(), delegate
                {
                    currentMode = CloneExtractorModes.Brain;
                    SelectPawn(selPawn);
                }), selPawn, this);
            }
            else if (base.SelectedPawn == selPawn && !selPawn.IsPrisonerOfColony)
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
            {
                yield return gizmo;
            }
            if (base.Working)
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
                if (DebugSettings.ShowDevGizmos)
                {
                    Command_Action command_Action2 = new Command_Action();
                    command_Action2.defaultLabel = "DEV: Finish extraction";
                    command_Action2.action = delegate
                    {
                        Finish();
                    };
                    yield return command_Action2;
                }
                yield break;
            }
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
                    sustainerWorking = null;
                };
                yield return command_Action3;
                yield break;
            }

            // Button to switch modes
            Command_Action toggleModeCommandAction = new Command_Action();
            toggleModeCommandAction.defaultLabel = "Cloning_CloneExtractorModeSwitch".Translate();
            toggleModeCommandAction.defaultDesc = "Cloning_InsertPersonBrainScanDesc".Translate();
            toggleModeCommandAction.icon = GeneSetHolderBase.GeneticInfoTex.Texture; //TODO: Replace this gizmo texture
            toggleModeCommandAction.action = delegate
            {
                List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();
                floatMenuOptions.Add(new FloatMenuOption("Cloning_EmbryoMode".Translate(), delegate
                {
                    currentMode = CloneExtractorModes.Embryo;
                }
                ));
                floatMenuOptions.Add(new FloatMenuOption("Cloning_BrainScanMode".Translate(), delegate
                {
                    currentMode = CloneExtractorModes.Brain;
                }
                ));

                if (!floatMenuOptions.Any())
                {
                    floatMenuOptions.Add(new FloatMenuOption("NoExtractablePawns".Translate(), null));
                }
                Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
            };
            if (this.Working)
            {
                toggleModeCommandAction.Disable("Cloning_ExtractorWorking".Translate().CapitalizeFirst());
            }
            yield return toggleModeCommandAction;

            Command_Action command_Action4 = new Command_Action();
            command_Action4.defaultLabel = "InsertPerson".Translate() + "...";
            command_Action4.defaultDesc = "InsertPersonGeneExtractorDesc".Translate();
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
                            currentMode = CloneExtractorModes.Embryo;
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

        public override void Draw()
        {
            base.Draw();
            if (!this.Working || this.selectedPawn == null || !this.innerContainer.Contains((Thing)this.selectedPawn))
                return;
            this.selectedPawn.Drawer.renderer.RenderPawnAt(this.DrawPos + this.PawnDrawOffset, neverAimWeapon: true);
        }

        public override string GetInspectString()
        {
            string str1 = base.GetInspectString();
            if (this.selectedPawn != null && this.innerContainer.Count == 0)
            {
                if (!str1.NullOrEmpty())
                    str1 += "\n";
                str1 += "WaitingForPawn".Translate(this.selectedPawn.Named("PAWN")).Resolve();
            }
            else if (this.Working && this.ContainedPawn != null)
            {
                if (!str1.NullOrEmpty())
                    str1 += "\n";
                string str2 = str1 + "Cloning_ExtractingCloneFrom".Translate(this.ContainedPawn.Named("PAWN")).Resolve() + "\n";
                str1 = !this.PowerOn ? (string)( str2 + "ExtractionPausedNoPower".Translate() ) : str2 + "DurationLeft".Translate((NamedArgument)this.ticksRemaining.ToStringTicksToPeriod()).Resolve();
                str1 = str1 + currentMode;
            }
            return str1;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining");
            Scribe_Values.Look<CloneExtractorModes>(ref this.currentMode, "currentMode");
        }
    }
}
