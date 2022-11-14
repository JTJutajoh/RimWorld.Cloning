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
    /// Based on vanilla Building_CloneExtractor, with a lot of copied functionality from Building_GeneAssembler<br />
    /// Kind of an unholy frankenstein of a class. A refactor would be nice someday
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
        private const int TicksToApplyScan = 30000;
        private const int TicksToMakeEmbryo = 30000;
        //private float WorkingPowerUsageFactor = 8f;
        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        public List<Thing> ConnectedFacilities => this.TryGetComp<CompAffectedByFacilities>().LinkedFacilitiesListForReading;

        // See: Building_GeneAssembler
        private List<Genepack> genepacksToRecombine;

        public Genepack donorGenepack;

        private int architesRequired;

        public string xenotypeName;

        public XenotypeIconDef iconDef;

        public XenotypeDef xenotypeDef;

        [Unsaved(false)]
        private List<Genepack> tmpGenepacks = new List<Genepack>();

        [Unsaved(false)]
        private HashSet<Thing> tmpUsedFacilities = new HashSet<Thing>();

        [Unsaved(false)]
        private int? cachedComplexity;

        private const int CheckContainersInterval = 180;

        public int ArchitesCount
        {
            get
            {
                int num = 0;
                for (int i = 0; i < innerContainer.Count; i++)
                {
                    if (innerContainer[i].def == ThingDefOf.ArchiteCapsule)
                    {
                        num += innerContainer[i].stackCount;
                    }
                }
                return num;
            }
        }

        private int TotalGCX
        {
            get
            {
                if (!Working)
                {
                    return 0;
                }
                if (!cachedComplexity.HasValue)
                {
                    cachedComplexity = 0;
                    if (!genepacksToRecombine.NullOrEmpty())
                    {
                        List<GeneDefWithType> list = new List<GeneDefWithType>();
                        for (int i = 0; i < genepacksToRecombine.Count; i++)
                        {
                            if (genepacksToRecombine[i].GeneSet != null)
                            {
                                for (int j = 0; j < genepacksToRecombine[i].GeneSet.GenesListForReading.Count; j++)
                                {
                                    list.Add(new GeneDefWithType(genepacksToRecombine[i].GeneSet.GenesListForReading[j], xenogene: true));
                                }
                            }
                        }
                        List<GeneDef> list2 = list.NonOverriddenGenes();
                        for (int k = 0; k < list2.Count; k++)
                        {
                            cachedComplexity += list2[k].biostatCpx;
                        }
                    }
                }
                return cachedComplexity.Value;
            }
        }
        public const int baseGCX = 0;


        public int ArchitesRequiredNow => architesRequired - ArchitesCount;


        //SOMEDAY: Make this a custom def instead of an enum
        public enum CloneExtractorModes
        {
            Embryo,
            Brain
        }
        [Unsaved(false)]
        private CloneExtractorModes currentMode = CloneExtractorModes.Embryo;

        #region Mostly Vanilla
        public Pawn ContainedPawn => this.innerContainer.Count <= 0 ? (Pawn)null : (Pawn)this.innerContainer[0];

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
                this.PowerTraderComp.PowerOutput = (this.Working ? -this.PowerComp.Props.PowerConsumption : -this.PowerComp.Props.idlePowerDraw);
            if (this.Working && this.PowerTraderComp.PowerOn && ArchitesRequiredNow <= 0)
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

        public List<Genepack> GetGenepacks(bool includePowered, bool includeUnpowered)
        {
            tmpGenepacks.Clear();
            if (donorGenepack != null) tmpGenepacks.Add(donorGenepack);
            List<Thing> connectedFacilities = ConnectedFacilities;
            if (connectedFacilities != null)
            {
                foreach (Thing item in connectedFacilities)
                {
                    CompGenepackContainer compGenepackContainer = item.TryGetComp<CompGenepackContainer>();
                    if (compGenepackContainer != null)
                    {
                        bool flag = item.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;
                        if (( includePowered && flag ) || ( includeUnpowered && !flag ))
                        {
                            tmpGenepacks.AddRange(compGenepackContainer.ContainedGenepacks);
                        }
                    }
                }
            }
            return tmpGenepacks;
        }
        public CompGenepackContainer GetGeneBankHoldingPack(Genepack pack)
        {
            List<Thing> connectedFacilities = ConnectedFacilities;
            if (connectedFacilities != null)
            {
                foreach (Thing item in connectedFacilities)
                {
                    CompGenepackContainer compGenepackContainer = item.TryGetComp<CompGenepackContainer>();
                    if (compGenepackContainer == null)
                    {
                        continue;
                    }
                    foreach (Genepack containedGenepack in compGenepackContainer.ContainedGenepacks)
                    {
                        if (containedGenepack == pack)
                        {
                            return compGenepackContainer;
                        }
                    }
                }
            }
            return null;
        }

        private void CheckAllContainersValid()
        {
            if (genepacksToRecombine.NullOrEmpty())
            {
                return;
            }
            List<Thing> connectedFacilities = ConnectedFacilities;
            for (int i = 0; i < genepacksToRecombine.Count; i++)
            {
                bool flag = false;
                for (int j = 0; j < connectedFacilities.Count; j++)
                {
                    CompGenepackContainer compGenepackContainer = connectedFacilities[j].TryGetComp<CompGenepackContainer>();
                    if (compGenepackContainer != null && compGenepackContainer.ContainedGenepacks.Contains(genepacksToRecombine[i]))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    Messages.Message("MessageXenogermCancelledMissingPack".Translate(this), this, MessageTypeDefOf.NegativeEvent);
                    Reset();
                    break;
                }
            }
        }
        #endregion Mostly Vanilla

        public int MaxComplexity()
        {
            List<Thing> connectedFacilities = ConnectedFacilities;
            if (connectedFacilities == null)
                return baseGCX;

            int result = baseGCX;
            foreach (Thing item in connectedFacilities)
            {
                CompPowerTrader compPowerTrader = item.TryGetComp<CompPowerTrader>();
                if (compPowerTrader == null || compPowerTrader.PowerOn)
                {
                    result += (int)item.GetStatValue(StatDefOf.GeneticComplexityIncrease);
                }
            }
            return result;
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
            return pawn.health.hediffSet.HasHediff(HediffDefOf.XenogerminationComa) ? (AcceptanceReport)"InXenogerminationComa".Translate() : (AcceptanceReport)true;
        }

        public void Cancel()
        {
            this.startTick = -1;
            this.selectedPawn = (Pawn)null;
            this.sustainerWorking = (Sustainer)null;
            this.xenotypeDef = null;
            this.xenotypeName = null;
            this.iconDef = null;
            this.architesRequired = 0;
            this.innerContainer.TryDropAll(this.def.hasInteractionCell ? this.InteractionCell : this.Position, this.Map, ThingPlaceMode.Near);
            donorGenepack?.Destroy();
            donorGenepack = null;
        }

        private HumanEmbryo ProduceEmbryo(Pawn donor, IntVec3 intVec3)
        {
            HumanEmbryo embryo;
            if (xenotypeDef != null)
            {
                embryo = CloneUtils.ProduceCloneEmbryo(this.ContainedPawn, xenotypeDef);
            }
            else
            {
                CustomXenotype customXenotype = new CustomXenotype();
                customXenotype.name = this.xenotypeName;
                customXenotype.iconDef = this.iconDef;
                customXenotype.genes = GeneUtils.GetAllGenesInPacks(genepacksToRecombine);
                customXenotype.genes.AddRange(GeneUtils.GetGeneDefsNamed(CloningSettings.genesForced));
                embryo = CloneUtils.ProduceCloneEmbryo(this.ContainedPawn, customXenotype);
            }
            
            this.xenotypeDef = null;
            this.xenotypeName = null;
            this.iconDef = null;

            // Gene sickness Hediff
            if (CloningSettings.cloningCooldown) 
                GeneUtility.ExtractXenogerm(donor, Mathf.RoundToInt(60000f * CloningSettings.CloneExtractorRegrowingDurationDaysRange.RandomInRange));


            this.innerContainer.TryDropAll(intVec3, this.Map, ThingPlaceMode.Near);

            if (!donor.Dead && ( donor.IsPrisonerOfColony || donor.IsSlaveOfColony ))
                donor.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.XenogermHarvested_Prisoner); //SOMEDAY: Make a new type of thought instead of using the xenogerm harvested thought

            GenPlace.TryPlaceThing((Thing)embryo, intVec3, this.Map, ThingPlaceMode.Near);

            Messages.Message((string)( "Cloning_CloneExtractionComplete".Translate(donor.Named("PAWN")) ),
                new LookTargets(new TargetInfo[2]
                    {
                        (TargetInfo) (Thing) donor,
                        (TargetInfo) (Thing) embryo
                    }
                ), MessageTypeDefOf.PositiveEvent);
            return embryo;
        }

        //TODO: Remove brain scanning drop
        /*private void ProduceBrainScan(Pawn donor, IntVec3 intVec3)
        {
            BrainScan brainScan = (BrainScan)ThingMaker.MakeThing(CloneDefOf.BrainScan);
            BrainUtil.ScanPawn(ContainedPawn, brainScan);
            this.innerContainer.TryDropAll(intVec3, this.Map, ThingPlaceMode.Near);
            //TODO: Add a thought about having had your brain scanned
            //if (!containedPawn.Dead && ( containedPawn.IsPrisonerOfColony || containedPawn.IsSlaveOfColony ))
            //    containedPawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.XenogermHarvested_Prisoner); //SOMEDAY: Make a new type of thought instead of using the xenogerm harvested thought
            GenPlace.TryPlaceThing((Thing)brainScan, intVec3, this.Map, ThingPlaceMode.Near);
            Messages.Message((string)( "Cloning_BrainScanComplete".Translate(donor.Named("PAWN")) ),
                new LookTargets(new TargetInfo[2]
                    {
                        (TargetInfo) (Thing) donor,
                        (TargetInfo) (Thing) brainScan
                    }
                ), MessageTypeDefOf.PositiveEvent);
        }*/

        private void Finish()
        {
            this.startTick = -1;
            this.selectedPawn = (Pawn)null;
            this.sustainerWorking = (Sustainer)null;
            if (this.ContainedPawn == null)
                return;
            Pawn containedPawn = this.ContainedPawn;
            IntVec3 intVec3 = this.def.hasInteractionCell ? this.InteractionCell : this.Position;
            if (this.architesRequired > 0)
            {
                for (int i = this.innerContainer.Count - 1; i >= 0; i--)
                {
                    if (this.innerContainer[i].def == ThingDefOf.ArchiteCapsule)
                    {
                        Thing thing = this.innerContainer[i].SplitOff(Mathf.Min(this.innerContainer[i].stackCount, this.architesRequired));
                        this.architesRequired -= thing.stackCount;
                        thing.Destroy(DestroyMode.Vanish);
                        if (this.architesRequired <= 0)
                            break;
                    }
                }
            }
            switch (currentMode)
            {
                case CloneExtractorModes.Embryo:
                    ProduceEmbryo(containedPawn, intVec3);
                    donorGenepack?.Destroy();
                    donorGenepack = null;

                    break;
                case CloneExtractorModes.Brain:
                    //ProduceBrainScan(containedPawn, intVec3);
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
            if (this.selectedPawn == null) return;
            this.selectedPawn = pawn;
            this.donorGenepack = GeneUtils.GetXenogenesAsGenepack(this.selectedPawn);
            Find.WindowStack.Add(new Dialog_CreateClone(this));
            this.selectedPawn = null;
        }

        public void AcceptPawn(Pawn pawn)
        {
            int num = pawn.DeSpawnOrDeselect() ? 1 : 0;
            if (this.innerContainer.TryAddOrTransfer((Thing)pawn))
            {
                this.startTick = Find.TickManager.TicksGame;
                this.ticksRemaining = Mathf.RoundToInt((float)Mathf.RoundToInt(GeneTuning.ComplexityToCreationHoursCurve.Evaluate(TotalGCX) * 2500f) / this.GetStatValue(StatDefOf.AssemblySpeedFactor));
            }
            if (num == 0)
                return;
            Find.Selector.Select((object)pawn, false, false);
        }

        private void Reset()
        {
            startTick = -1;
            genepacksToRecombine = null;
            xenotypeName = null;
            cachedComplexity = null;
            iconDef = XenotypeIconDefOf.Basic;
            architesRequired = 0;
            //donorGenepack?.Destroy();
            innerContainer.TryDropAll(def.hasInteractionCell ? InteractionCell : base.Position, base.Map, ThingPlaceMode.Near);
        }

        public void Start(List<Genepack> packs, Pawn pawn, int architesRequired, string xenotypeName, XenotypeIconDef iconDef, XenotypeDef xenotypeDef)
        {
            Reset();
            genepacksToRecombine = packs;
            this.architesRequired = architesRequired;
            this.xenotypeName = xenotypeName;
            this.iconDef = iconDef;
            this.xenotypeDef = xenotypeDef;
            this.selectedPawn = pawn;
            AcceptPawn(selectedPawn);
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
                //TODO: Brain scanning float menu option disabled on Clone Extractor
                /*yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this) + " " + "Mode_BrainScan".Translate(), delegate
                {
                    currentMode = CloneExtractorModes.Brain;
                    SelectPawn(selPawn);
                }), selPawn, this);*/
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
            if (DebugSettings.ShowDevGizmos && base.Working)
            {
                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "DEV: Finish extraction";
                command_Action2.action = delegate
                {
                    Finish();
                };
                yield return command_Action2;
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
                        sustainerWorking = null;
                    };
                    yield return command_Action3;
                    yield break;
                }

                // Button to switch modes
                //TODO: Clone Extractor mode switch gizmo disabled until brain scanning is ready
                /*Command_Action toggleModeCommandAction = new Command_Action();
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
                yield return toggleModeCommandAction;*/

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
            }
            str1 += "\n";
            str1 += "MaxGCX".Translate() + ": " + this.MaxComplexity();
            return str1;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksRemaining, "ticksRemaining");
            Scribe_Values.Look<CloneExtractorModes>(ref this.currentMode, "currentMode");

            //Scribe_Deep.Look<ThingOwner>(ref innerContainer, "innerContainer", new object[] { this });
            Scribe_Collections.Look(ref genepacksToRecombine, "genepacksToRecombine", LookMode.Reference);
            Scribe_Values.Look(ref architesRequired, "architesRequired", 0);
            Scribe_Values.Look(ref xenotypeName, "xenotypeName");
            Scribe_Defs.Look(ref iconDef, "iconDef");
            Scribe_Defs.Look(ref xenotypeDef, "xenotypeDef");
            Scribe_Deep.Look(ref donorGenepack, "donorGenepack");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && iconDef == null)
            {
                iconDef = XenotypeIconDefOf.Basic;
            }
        }
    }
}
