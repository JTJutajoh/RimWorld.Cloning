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
    public class Building_CloneVat : Building_Enterable, IThingHolderWithDrawnPawn, IThingHolder
	{
		public BrainScan selectedBrainScan;

		[Unsaved(false)]
		private CompPowerTrader cachedPowerComp;

		[Unsaved(false)]
		private Graphic cachedTopGraphic;

		[Unsaved(false)]
		private Sustainer sustainerWorking;

		[Unsaved(false)]
		private Effecter bubbleEffecter;

		private static readonly Texture2D CancelLoadingIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

		public static readonly CachedTexture InsertPawnIcon = new CachedTexture("UI/Gizmos/InsertPawn");

		public static readonly CachedTexture InsertBrainScanIcon = new CachedTexture("UI/Gizmos/InsertEmbryo");

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
					cachedTopGraphic = GraphicDatabase.Get<Graphic_Multi>("Things/Building/Misc/GrowthVat/GrowthVatTop", ShaderDatabase.Transparent, def.graphicData.drawSize, Color.green);
				}
				return cachedTopGraphic;
			}
		}

		public bool PowerOn => PowerTraderComp.PowerOn;

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

			if (base.Working)
			{
				if (selectedPawn != null)
				{
					
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
				if (this.IsHashIntervalTick(132))
				{
					MoteMaker.MakeStaticMote(DrawPos, base.MapHeld, GlowMotePerRotation[base.Rotation]);
				}
				if (bubbleEffecter == null)
				{
					bubbleEffecter = BubbleEffecterPerRotation[base.Rotation].SpawnAttached(this, base.MapHeld);
				}
				bubbleEffecter.EffectTick(this, this);
			}
			else
			{
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
				startTick = Find.TickManager.TicksGame;
			}
			if (num)
			{
				Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
			}
		}

		private void Finish()
		{
			if (selectedPawn != null)
			{
				FinishPawn();
			}
			if (selectedBrainScan != null && innerContainer.Contains(selectedBrainScan))
            {
				innerContainer.TryDrop(selectedBrainScan, InteractionCell, base.Map, ThingPlaceMode.Near, 1, out var _);
			}
			
			OnStop();
		}

		private void FinishPawn()
		{
			if (selectedPawn != null && innerContainer.Contains(selectedPawn))
			{
				Notify_PawnRemoved();
				if (selectedPawn.RaceProps.IsFlesh)
                {
					selectedPawn.health.AddHediff(HediffDefOf.CryptosleepSickness);
                }
				innerContainer.TryDrop(selectedPawn, InteractionCell, base.Map, ThingPlaceMode.Near, 1, out var _);
			}
		}

		private void OnStop()
		{
			selectedPawn = null;
			selectedBrainScan = null;
			startTick = -1;
			sustainerWorking = null;
		}

		private void Notify_PawnRemoved()
		{
			SoundDefOf.GrowthVat_Open.PlayOneShot(SoundInfo.InMap(this));
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
				command_Action.defaultLabel = "CommandCancelGrowth".Translate();
				command_Action.defaultDesc = "CommandCancelGrowthDesc".Translate();
				command_Action.icon = CancelLoadingIcon;
				command_Action.activateSound = SoundDefOf.Designate_Cancel;
				command_Action.action = delegate
				{
					Finish();
					innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
				};
				yield return command_Action;
			}
			else
			{
				if (selectedPawn != null)
				{
					Command_Action command_Action2 = new Command_Action();
					command_Action2.defaultLabel = "CommandCancelLoad".Translate();
					command_Action2.defaultDesc = "CommandCancelLoadDesc".Translate();
					command_Action2.icon = CancelLoadingIcon;
					command_Action2.activateSound = SoundDefOf.Designate_Cancel;
					command_Action2.action = delegate
					{
						innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
						if (innerContainer.Contains(selectedPawn))
						{
							Notify_PawnRemoved();
						}
						if (selectedPawn?.CurJobDef == JobDefOf.EnterBuilding)
						{
							selectedPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
						}
						OnStop();
					};
					yield return command_Action2;
				}
				if (selectedPawn == null)
				{
					Command_Action command_Action3 = new Command_Action();
					command_Action3.defaultLabel = "InsertPerson".Translate() + "...";
					command_Action3.defaultDesc = "InsertPersonGrowthVatDesc".Translate();
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
						command_Action3.Disable("NoPawnsCanEnterGrowthVat".Translate(18f).ToString());
					}
					yield return command_Action3;
				}
			}
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
						options.Add(new FloatMenuOption(brainScan.LabelCap + " (" + ((BrainScan)brainScan).sourceLabel + ")", delegate
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
			else
            {
				Command_Action cancelScan = new Command_Action();
				cancelScan.defaultLabel = innerContainer.Contains(selectedBrainScan) ?  "RemoveBrainScan".Translate() : "CancelBrainScan".Translate();
				cancelScan.defaultDesc = "CancelBrainScanDesc".Translate();
				cancelScan.icon = CancelLoadingIcon;
				cancelScan.action = delegate
				{
					Finish();
				};
				yield return cancelScan;
            }
			Command_Action applyScan = new Command_Action();
			applyScan.defaultLabel = "ApplyBrainScan".Translate();
			applyScan.defaultDesc = "ApplyBrainScanDesc".Translate();
			applyScan.icon = InsertBrainScanIcon.Texture;
			applyScan.action = delegate
			{
				//TODO: Add a warning confirmation dialog reminding the player that the pawn will effectively be killed by this procedure and it is not reversible
				ApplyBrainScanToPawn();
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

		public void SelectBrainScan(BrainScan brainScan)
        {
			selectedBrainScan = brainScan;
        }

		public override void Draw()
		{
			base.Draw();
			if (base.Working)
			{
				if (selectedPawn != null)
				{
					if (innerContainer.Contains(selectedPawn))
					{
						selectedPawn.Drawer.renderer.RenderPawnAt(DrawPos + PawnDrawOffset, null, neverAimWeapon: true);
					}
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

		public void ApplyBrainScanToPawn()
        {
			Pawn pawn = SelectedPawn;
			BrainScan brainScan = selectedBrainScan;

			// Name
			pawn.Name = brainScan.sourceName;

			// Backstories
			if (pawn.story != null)
            {
				pawn.story.Childhood = brainScan.backStoryChild;
				pawn.story.Adulthood = brainScan.backStoryAdult;
            }
			
			// Traits
			// First clear the old traits
			List<Trait> tmpList = new List<Trait>(pawn.story.traits.allTraits);
			foreach (Trait trait in tmpList)
            {
				pawn.story.traits.RemoveTrait(trait);
			}
			// Then add the new ones
			foreach (TraitBackup trait in brainScan.traits)
            {
				pawn.story.traits.GainTrait(new Trait(trait.def, trait.degree));
            }

			// Skills
			for (int i = 0; i < brainScan.skills.Count; i++)
            {
				SkillRecordBackup scannedSkill = brainScan.skills[i];
				SkillRecord skill = pawn.skills.GetSkill(scannedSkill.def);
				skill.Level = scannedSkill.level; //SOMEDAY: Maybe an efficiency setting for brain scan skill copying?
				skill.passion = scannedSkill.passion;
				skill.xpSinceLastLevel = scannedSkill.xpSinceLastLevel;
				skill.Notify_SkillDisablesChanged();
            }

			// Social relations
			//TODO: Copy social relations
        }

		public override void ExposeData()
		{
			base.ExposeData();
			
		}
	}
}
