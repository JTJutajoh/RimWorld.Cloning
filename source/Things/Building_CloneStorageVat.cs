using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Sound;
using Verse.AI;
using UnityEngine;

namespace Dark.Cloning
{
	// Based on the vanilla GrowthVat class, with different behavior
	//TODO: Clone Storage Vat disabled. Possibly pending deletion
	/*public class Building_CloneStorageVat : Building_Enterable, IThingHolderWithDrawnPawn, IThingHolder, IOpenable
	{
		public BrainScan selectedBrainScan;

		private const int scanTicks = 1000;
		private int ticksRemaining = 0;

		[Unsaved(false)]
		private CompPowerTrader cachedPowerComp;

		[Unsaved(false)]
		private Graphic cachedTopGraphic;

		[Unsaved(false)]
		private Sustainer sustainerWorking;

		[Unsaved(false)]
		private Effecter bubbleEffecter;

		[Unsaved(false)]
		private Effecter progressBar;

		private static readonly Texture2D CancelLoadingIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

		public static readonly CachedTexture InsertPawnIcon = new CachedTexture("UI/Gizmos/InsertPawn");

		public static readonly CachedTexture InsertBrainScanIcon = new CachedTexture("UI/Gizmos/InsertBrainScan");

		private const int GlowIntervalTicks = 132;

		private static Dictionary<Rot4, ThingDef> GlowMotePerRotation;

		private static Dictionary<Rot4, EffecterDef> BubbleEffecterPerRotation;

		public float HeldPawnDrawPos_Y => DrawPos.y + 3f / 74f;

		public float HeldPawnBodyAngle => base.Rotation.AsAngle;

		public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

		public override Vector3 PawnDrawOffset => CompBiosculpterPod.FloatingOffset(Find.TickManager.TicksGame);

		private CompPowerTrader PowerTraderComp
		{
			get
			{
				if (cachedPowerComp == null)
				{
					cachedPowerComp = this.TryGetComp<CompPowerTrader>();
				}
				return cachedPowerComp;
			}
		}

		private Graphic TopGraphic
		{
			get
			{
				if (cachedTopGraphic == null)
				{
					cachedTopGraphic = GraphicDatabase.Get<Graphic_Multi>("Things/Building/Misc/CloneStorageVat/CloneStorageVatTop", ShaderDatabase.Transparent, def.graphicData.drawSize, Color.white);
				}
				return cachedTopGraphic;
			}
		}

		public bool PowerOn => PowerTraderComp.PowerOn;

		public bool CanOpen => (!base.Working && selectedPawn != null && innerContainer.Contains(selectedPawn));

        public int OpenTicks => 200;

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			if (selectedPawn != null && innerContainer.Contains(selectedPawn))
			{
				Notify_PawnRemoved();
			}
			sustainerWorking = null;
			base.DeSpawn(mode);
		}

		public override void Tick()
		{
			base.Tick();
			innerContainer.ThingOwnerTick();
			if (this.IsHashIntervalTick(250))
			{
				PowerTraderComp.PowerOutput = 0f - ( base.Working ? base.PowerComp.Props.PowerConsumption : base.PowerComp.Props.idlePowerDraw );
			}
			Pawn pawn = selectedPawn;

			if (selectedPawn != null && innerContainer.Contains(selectedPawn))
			{
				if (bubbleEffecter == null)
				{
					if (BubbleEffecterPerRotation == null)
                    {
						BubbleEffecterPerRotation = new Dictionary<Rot4, EffecterDef>
						{
							{
								Rot4.South,
								EffecterDefOf.Vat_Bubbles_South
							},
							{
								Rot4.East,
								EffecterDefOf.Vat_Bubbles_East
							},
							{
								Rot4.West,
								EffecterDefOf.Vat_Bubbles_West
							},
							{
								Rot4.North,
								EffecterDefOf.Vat_Bubbles_North
							}
						};
					}
					bubbleEffecter = BubbleEffecterPerRotation[base.Rotation].SpawnAttached(this, base.MapHeld);
				}
				bubbleEffecter.EffectTick(this, this);
			}
			if (base.Working)
			{
				if (PowerOn)
                {
					ticksRemaining--;
				}
				if (ticksRemaining <= 0)
				{
					if (selectedPawn != null && innerContainer.Contains(selectedPawn) && selectedBrainScan != null && innerContainer.Contains(selectedBrainScan))
					{
						BrainUtil.ApplyBrainScanToPawn(selectedPawn, selectedBrainScan);
						OnStopScanApplication();
					}
					else
                    {
						Log.Error("Clone Storage Vat tried to complete a brain scan, but didn't contain either a pawn or a brain scan");
                    }
				}
				if (sustainerWorking == null || sustainerWorking.Ended)
				{
					sustainerWorking = SoundDefOf.GrowthVat_Working.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
				}
				else
				{
					sustainerWorking.Maintain();
				}
				if (GlowMotePerRotation == null)
				{
					GlowMotePerRotation = new Dictionary<Rot4, ThingDef>
				{
					{
						Rot4.South,
						ThingDefOf.Mote_VatGlowVertical
					},
					{
						Rot4.East,
						ThingDefOf.Mote_VatGlowHorizontal
					},
					{
						Rot4.West,
						ThingDefOf.Mote_VatGlowHorizontal
					},
					{
						Rot4.North,
						ThingDefOf.Mote_VatGlowVertical
					}
				};
				}
				if (this.IsHashIntervalTick(132))
				{
					MoteMaker.MakeStaticMote(DrawPos, base.MapHeld, GlowMotePerRotation[base.Rotation]);
				}
				if (progressBar == null)
				{
					progressBar = EffecterDefOf.ProgressBarAlwaysVisible.Spawn();
				}
				progressBar.EffectTick(new TargetInfo(base.Position + IntVec3.North.RotatedBy(base.Rotation), base.Map), TargetInfo.Invalid);
				MoteProgressBar mote = ( (SubEffecter_ProgressBar)progressBar.children[0] ).mote;
				if (mote != null)
				{
					mote.progress = 1f - Mathf.Clamp01((float)ticksRemaining / scanTicks);
					mote.offsetZ = ( ( base.Rotation == Rot4.North ) ? 0.5f : ( -0.5f ) );
				}
			}
			else
			{
				if (progressBar != null)
				{
					progressBar.Cleanup();
					progressBar = null;
				}
				bubbleEffecter?.Cleanup();
				bubbleEffecter = null;
			}
		}

		public override AcceptanceReport CanAcceptPawn(Pawn pawn)
		{
			if (base.Working)
			{
				return "Occupied".Translate();
			}
			if (!PowerOn)
			{
				return "NoPower".Translate().CapitalizeFirst();
			}
			if (selectedPawn != null && selectedPawn != pawn)
			{
				return "WaitingForPawn".Translate(selectedPawn.Named("PAWN"));
			}
			if (pawn.health.hediffSet.HasHediff(HediffDefOf.BioStarvation))
			{
				return "PawnBiostarving".Translate(pawn.Named("PAWN"));
			}
			return pawn.IsColonist && !pawn.IsQuestLodger();
		}

		public override void TryAcceptPawn(Pawn pawn)
		{
			if (selectedPawn == null || !CanAcceptPawn(pawn))
			{
				return;
			}
			selectedPawn = pawn;
			bool num = pawn.DeSpawnOrDeselect();
			if (innerContainer.TryAddOrTransfer(pawn))
			{
				SoundDefOf.GrowthVat_Close.PlayOneShot(SoundInfo.InMap(this));
			}
			if (num)
			{
				Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
			}
		}

		private void StartApplyingBrainScan()
		{
			if (!base.Working && PowerOn && selectedBrainScan != null && innerContainer.Contains(selectedBrainScan) && selectedPawn != null && innerContainer.Contains(SelectedPawn))
			{
				startTick = Find.TickManager.TicksGame + scanTicks;
				ticksRemaining = scanTicks;
			}
		}

		private void EjectBrainScan()
        {
			if (selectedBrainScan != null && innerContainer.Contains(selectedBrainScan))
            {
				innerContainer.TryDrop(selectedBrainScan, InteractionCell, base.Map, ThingPlaceMode.Near, 1, out var _);
				selectedBrainScan = null;
				startTick = -1;
			}
        }

		private void EjectPawn()
		{
			if (selectedPawn != null && innerContainer.Contains(selectedPawn))
			{
				innerContainer.TryDrop(selectedPawn, InteractionCell, base.Map, ThingPlaceMode.Near, 1, out var pawn);
				ThingDef filth_Slime = ThingDefOf.Filth_Slime;
				selectedPawn.filth?.GainFilth(filth_Slime); //FIXME: filth is null here for some reason
				Notify_PawnRemoved();
				if (selectedPawn.RaceProps.IsFlesh)
				{
					selectedPawn.health?.AddHediff(HediffDefOf.CryptosleepSickness);
				}
				selectedPawn = null;
				startTick = -1;
			}
		}

		private void OnStopScanApplication()
		{
			startTick = -1;
			sustainerWorking = null;
		}

		private void Notify_PawnRemoved()
		{
			SoundDefOf.GrowthVat_Open.PlayOneShot(SoundInfo.InMap(this));
		}

		// Used in a Harmony Patch to cancel the haul job, by adding a new FailOn to MakeNewToils
		public static bool WasHaulJobCanceled(Thing thing, Thing thingToCarry)
        {
			var cloneVat = thing as Building_CloneStorageVat;
			if (cloneVat == null)
            {
				return false;
            }
			if (cloneVat.selectedBrainScan == null)
            {
				return true;
            }
			var brainScan = thingToCarry as BrainScan;
			if (brainScan == null)
            {
				return true;
            }
			if (cloneVat.selectedBrainScan != brainScan)
            {
				return true;
            }

			return false;
        }

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			// If there is no scan underway
			if (!base.Working)
            {
				// Eject pawn
				if (selectedPawn != null && innerContainer.Contains(selectedPawn))
				{
					Command_Action command_Action = new Command_Action();
					command_Action.defaultLabel = "EjectClone".Translate();
					command_Action.defaultDesc = "EjectCloneDesc".Translate();
					command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject");
					command_Action.activateSound = SoundDefOf.Designate_Cancel;
					command_Action.action = delegate
					{
						EjectPawn();
					};
					//yield return command_Action;
					// Commented out in favor of implementing IOpenable and letting it handle this.
				}
				// Pawn loading
				else
				{
					// Cancel load
					if (selectedPawn != null)
					{
						Command_Action command_Action2 = new Command_Action();
						command_Action2.defaultLabel = "CancelLoadPawn".Translate();
						command_Action2.defaultDesc = "CancelLoadPawnDesc".Translate();
						command_Action2.icon = CancelLoadingIcon;
						command_Action2.activateSound = SoundDefOf.Designate_Cancel;
						command_Action2.action = delegate
						{
							innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
							if (innerContainer.Contains(selectedPawn))
							{
								EjectPawn();
							}
							if (selectedPawn?.CurJobDef == JobDefOf.EnterBuilding)
							{
								selectedPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
							}
							selectedPawn = null;
						};
						yield return command_Action2;
					}
					// Load pawn
					if (selectedPawn == null)
					{
						Command_Action command_Action3 = new Command_Action();
						command_Action3.defaultLabel = "InsertClone".Translate() + "...";
						command_Action3.defaultDesc = "InsertCloneDesc".Translate();
						command_Action3.icon = InsertPawnIcon.Texture;
						command_Action3.action = delegate
						{
							List<FloatMenuOption> list2 = new List<FloatMenuOption>();
							foreach (Pawn item2 in base.Map.mapPawns.AllPawnsSpawned)
							{
								Pawn pawn = item2;
								if ((bool)CanAcceptPawn(item2))
								{
									list2.Add(new FloatMenuOption(item2.LabelCap, delegate
									{
										SelectPawn(pawn);
									}, pawn, Color.white));
								}
							}
							if (!list2.Any())
							{
								list2.Add(new FloatMenuOption("NoViablePawns".Translate(), null));
							}
							Find.WindowStack.Add(new FloatMenu(list2));
						};
						if (!PowerOn)
						{
							command_Action3.Disable("NoPower".Translate().CapitalizeFirst());
						}
						else if (!base.AnyAcceptablePawns)
						{
							command_Action3.Disable("NoPawnsCanEnterCloneVat".Translate(18f).ToString());
						}
						yield return command_Action3;
					}
				}
				// BrainScan loading
				if (selectedBrainScan == null)
				{
					List<Thing> brainScans = base.Map.listerThings.ThingsOfDef(CloneDefOf.BrainScan);
					Command_Action installBrainScan = new Command_Action();
					installBrainScan.defaultLabel = "InstallBrainScan".Translate() + "...";
					installBrainScan.defaultDesc = "InsertBrainScanCloneVatDesc".Translate();
					installBrainScan.icon = InsertBrainScanIcon.Texture;
					installBrainScan.action = delegate
					{
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						foreach (Thing brainScan in brainScans)
						{
							options.Add(new FloatMenuOption(brainScan.LabelCap + " (" + ( (BrainScan)brainScan ).sourceLabel + ")", delegate
							{
								SelectBrainScan(brainScan as BrainScan);
							}, brainScan, Color.white));
						}
						Find.WindowStack.Add(new FloatMenu(options));
					};
					if (brainScans.NullOrEmpty())
					{
						installBrainScan.Disable("InsertBrainScanNoScans".Translate().CapitalizeFirst());
					}
					else if (!PowerOn)
					{
						installBrainScan.Disable("NoPower".Translate().CapitalizeFirst());
					}
					yield return installBrainScan;
				}
				// BrainScan loading/unloading
				else
				{
					// Eject BrainScan
					if (innerContainer.Contains(selectedBrainScan))
					{
						Command_Action removeScan = new Command_Action();
						removeScan.defaultLabel = "RemoveBrainScan".Translate();
						removeScan.defaultDesc = "CancelBrainScanDesc".Translate();
						removeScan.icon = CancelLoadingIcon;
						removeScan.action = delegate
						{
							EjectBrainScan();
						};
						yield return removeScan;
					}
					// Cancel loading
					else
					{
						Command_Action cancelScan = new Command_Action();
						cancelScan.defaultLabel = "CancelBrainScan".Translate();
						cancelScan.defaultDesc = "CancelBrainScanDesc".Translate();
						cancelScan.icon = CancelLoadingIcon;
						cancelScan.action = delegate
						{
							selectedBrainScan = null;
						};
						yield return cancelScan;
					}
				}
			}
			
			// Cancel scan application
			if (base.Working)
            {
				Command_Action cancelScanApplication = new Command_Action();
				cancelScanApplication.defaultLabel = "CancelScanApplication".Translate();
				cancelScanApplication.defaultDesc = "CancelScanApplicationDesc".Translate();
				cancelScanApplication.icon = CancelLoadingIcon;
				cancelScanApplication.action = delegate
				{
					OnStopScanApplication();
				};
				yield return cancelScanApplication;
			}
			// BrainScan application
			else if (selectedBrainScan != null && innerContainer.Contains(selectedBrainScan))
			{
				Command_Action applyScan = new Command_Action();
				applyScan.defaultLabel = "ApplyBrainScan".Translate();
				applyScan.defaultDesc = "ApplyBrainScanDesc".Translate();
				applyScan.icon = InsertBrainScanIcon.Texture;
				applyScan.action = delegate
				{
					Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmBrainScanApplication".Translate(SelectedPawn.Named("PAWN")), delegate
					{
						StartApplyingBrainScan();
					}));
				};
				if (selectedBrainScan == null || !innerContainer.Contains(selectedBrainScan))
				{
					applyScan.Disable("NoBrainScan".Translate());
				}
				if (selectedPawn == null || !innerContainer.Contains(selectedPawn))
				{
					applyScan.Disable("NoPawnToApplyTo".Translate());
				}
				//TODO: Disable brain scan application for other reasons, like target is already the same pawn stored in the scan or target does not have Clone gene
				yield return applyScan;
			}
		}

		public void SelectBrainScan(BrainScan brainScan)
        {
			selectedBrainScan = brainScan;
        }

		public override void Draw()
		{
			base.Draw();
			if (selectedPawn != null)
			{
				if (innerContainer.Contains(selectedPawn))
				{
					selectedPawn.Drawer.renderer.RenderPawnAt(DrawPos + PawnDrawOffset, null, neverAimWeapon: true);
				}
			}
			TopGraphic.Draw(DrawPos + Altitudes.AltIncVect * 2f, base.Rotation, this);
		}

		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.GetInspectString());
			if (selectedPawn != null && innerContainer.Contains(selectedPawn))
			{
				stringBuilder.AppendLineIfNotEmpty().Append(string.Format("{0}: {1}, {2}", "CasketContains".Translate().ToString(), selectedPawn.NameShortColored.Resolve(), selectedPawn.ageTracker.AgeBiologicalYears));
			}
			else if (selectedPawn != null)
			{
				stringBuilder.AppendLineIfNotEmpty().Append("WaitingForPawn".Translate(selectedPawn.Named("PAWN")).Resolve());
			}

			if (selectedBrainScan != null && innerContainer.Contains(selectedBrainScan))
            {
				stringBuilder.AppendLineIfNotEmpty().Append("ContainsBrainScan".Translate() + ": " + selectedBrainScan.sourceLabel);
            }
			else if (selectedBrainScan != null)
            {
				stringBuilder.AppendLineIfNotEmpty().Append("WaitingForBrainScan".Translate());
            }
			return stringBuilder.ToString();
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
				yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
				{
					SelectPawn(selPawn);
				}), selPawn, this);
			}
			else if (!acceptanceReport.Reason.NullOrEmpty())
			{
				yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + acceptanceReport.Reason.CapitalizeFirst(), null);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();

			Scribe_References.Look(ref this.selectedBrainScan, "selectedBrainScan");
			Scribe_References.Look(ref this.selectedPawn, "selectedPawn");
		}

        public void Open()
        {
			EjectPawn();
        }
    }*/
}