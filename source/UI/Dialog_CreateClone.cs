using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;


namespace Dark.Cloning
{
    public class Dialog_CreateClone : GeneCreationDialogBase
    {
		private Building_CloneExtractor cloneExtractor;

		private Pawn donorPawn;

		public XenotypeDef xenotypeDef;

		public bool UniqueXenotype;

		private bool mouseOverAnyDonorGene;
		private bool highlightAllDonorGenes;

		private List<Genepack> libraryGenepacks = new List<Genepack>();

		private List<Genepack> donorGenepacks = new List<Genepack>();

		private List<Genepack> unpoweredGenepacks = new List<Genepack>();

		private List<Genepack> selectedGenepacks = new List<Genepack>();

		private HashSet<Genepack> matchingGenepacks = new HashSet<Genepack>();

		private readonly Color UnpoweredColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

		private List<GeneDef> tmpGenes = new List<GeneDef>();

		private bool started = false;

		public override Vector2 InitialSize => new Vector2(1016f, UI.screenHeight);

		protected override string Header => "AssembleGenes".Translate();

		protected override string AcceptButtonLabel => "StartCombining".Translate();

		protected override List<GeneDef> SelectedGenes
		{
			get
			{
				tmpGenes.Clear();
				foreach (Genepack selectedGenepack in selectedGenepacks)
				{
					foreach (GeneDef item in selectedGenepack.GeneSet.GenesListForReading)
					{
						tmpGenes.Add(item);
					}
				}
				return tmpGenes;
			}
		}
		public Dialog_CreateClone(Building_CloneExtractor cloneExtractor)
		{
			this.cloneExtractor = cloneExtractor;
			maxGCX = cloneExtractor.MaxComplexity();
			donorPawn = cloneExtractor.SelectedPawn;
			Genepack donorGenePack = cloneExtractor.donorGenepack;
			if (donorGenePack != null)
			{
				donorGenepacks.Add(donorGenePack);
				selectedGenepacks.Add(donorGenePack);
			}
			libraryGenepacks.AddRange(cloneExtractor.GetGenepacks(includePowered: true, includeUnpowered: true));
			unpoweredGenepacks.AddRange(cloneExtractor.GetGenepacks(includePowered: false, includeUnpowered: true));
			closeOnAccept = false;
			forcePause = true;
			absorbInputAroundWindow = true;
			searchWidgetOffsetX = GeneCreationDialogBase.ButSize.x * 2f + 4f;
			libraryGenepacks.SortGenepacks();
			unpoweredGenepacks.SortGenepacks();

			GetPawnXenotype();
		}

		private void GetPawnXenotype()
		{
			if (donorPawn.genes.UniqueXenotype)
            {
				xenotypeName = donorPawn.genes.xenotypeName;
				iconDef = donorPawn.genes.iconDef;
				UniqueXenotype = true;
            }
			else
            {
				xenotypeDef = donorPawn.genes.Xenotype;
				xenotypeName = donorPawn.genes.XenotypeLabel;
				//FIXME: Can't get the iconDef of a XenotypeDef, so the xenotype icon in the bottom corner will be wrong. 
				// Maybe do a crazy hack of creating a new XenotypeIconDef on the fly and assigning its icon path? This probably won't work for some reason
				// The other alternative I can think of is to either patch or override (if possible) the method that draws the icon in GeneCreationDialogBase to be able to show the correct icon
				//iconDef = donorPawn.genes.iconDef;
			}
		}

		public override void PostOpen()
		{
			if (!ModLister.CheckBiotech("clone assembly"))
			{
				Close(doCloseSound: false);
			}
			else
			{
				base.PostOpen();
			}
		}

		protected override void Accept()
		{
			if (cloneExtractor.Working)
			{
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmStartNewXenogerm".Translate(cloneExtractor.xenotypeName.Named("XENOGERMNAME")), StartAssembly, destructive: true)); //TEMP: Vanilla xenogerm confirmation dialog
			}
			else
			{
				StartAssembly();
			}
		}

        public override void Close(bool doCloseSound = true)
        {
			if (!started)
			{
				cloneExtractor.Cancel();
			}

            base.Close(doCloseSound);
        }

        private void StartAssembly()
		{
			started = true;
			cloneExtractor.Start(selectedGenepacks, donorPawn, CloningSettings.architeGenesFree ? 0 : arc, xenotypeName?.Trim(), iconDef, !UniqueXenotype ? xenotypeDef : null);
			SoundDefOf.StartRecombining.PlayOneShotOnCamera();
			Close(doCloseSound: false);
		}

		protected override void DrawGenes(Rect rect)
		{
			GUI.BeginGroup(rect);
			Rect rect2 = new Rect(0f, 0f, rect.width - 16f, scrollHeight);
			float curY = 0f;
			Widgets.BeginScrollView(rect.AtZero(), ref scrollPosition, rect2);
			Rect containingRect = rect2;
			containingRect.y = scrollPosition.y;
			containingRect.height = rect.height;
			DrawCloneSection(rect, "SelectedGenepacks".Translate(), ref curY, ref selectedHeight, containingRect);
			curY += 8f;
			Rect donorRect = rect;
			donorRect.width -= 56f;
			donorRect.x += 12f;
			DrawDonorSection(donorRect, ref curY, ref unselectedHeight, containingRect);
			curY += 8f;
			DrawLibrarySection(rect, "GenepackLibrary".Translate(), ref curY, ref unselectedHeight, containingRect);
			if (Event.current.type == EventType.Layout)
			{
				scrollHeight = curY;
			}
			Widgets.EndScrollView();
			GUI.EndGroup();
		}

		private void DrawCloneSection(Rect rect, string label, ref float curY, ref float sectionHeight, Rect containingRect)
        {
			float curX = 4f;
			Rect labelRect = new Rect(10f, curY, rect.width - 16f - 10f, Text.LineHeight);
			Widgets.Label(labelRect, label);

			Text.Anchor = TextAnchor.UpperRight;
			GUI.color = ColoredText.SubtleGrayColor;
			Widgets.Label(labelRect, "ClickToAddOrRemove".Translate());
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;

			curY += Text.LineHeight + 3f;
			float startY = curY;
			float startX = curX;
			Rect sectionRect = new Rect(0f, curY, rect.width, sectionHeight);
			Widgets.DrawRectFast(sectionRect, Widgets.MenuSectionBGFillColor);
			curY += 4f;

			List<Gene> endogenes = donorPawn.genes.Endogenes;

			if (!selectedGenepacks.Any() && !endogenes.Any())
			{
				Text.Anchor = TextAnchor.MiddleCenter;
				GUI.color = ColoredText.SubtleGrayColor;
				Widgets.Label(sectionRect, "(" + "NoneLower".Translate() + ")");
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			else
			{
				// Draw endogenes
				if (endogenes != null)
				{
					for (int i = 0; i < endogenes.Count; i++)
					{
						float width = GeneCreationDialogBase.GeneSize.x + 12f;
						DrawGeneasGenepack(endogenes[i].def, ref curX, curY, width, GeneType.Endogene, true, containingRect);
						if (curX + width > rect.width - 16f)
						{
							curX = 4f;
							curY += GeneCreationDialogBase.GeneSize.y + 8f + 14f;
						}
					}
				}
				// Draw xenogenes
				for (int i = 0; i < selectedGenepacks.Count; i++)
				{
					Genepack genepack = selectedGenepacks[i];
					// First, if this genepack is the donor genepack, instead of drawing it, draw all the individual genes in it as if they were genepacks
					if (donorGenepacks.Contains(genepack))
					{
						for (int j = 0; j < genepack.GeneSet.GenesListForReading.Count; j++)
						{
							float width = 34f + GeneCreationDialogBase.GeneSize.x + 12f;
							if (DrawGeneasGenepack(genepack.GeneSet.GenesListForReading[j], ref curX, curY, width, GeneType.Xenogene, true, containingRect, true, genepack))
							{
								this.selectedGenepacks.Remove(genepack);
								OnGenesChanged();
							}
							if (curX + width > rect.width - 16f)
							{
								curX = 4f;
								curY += GeneCreationDialogBase.GeneSize.y + 8f + 14f;
							}
						}
						continue;
					}
					float packWidth = 34f + GeneCreationDialogBase.GeneSize.x * (float)genepack.GeneSet.GenesListForReading.Count + 4f * (float)( genepack.GeneSet.GenesListForReading.Count + 2 );
					if (curX + packWidth > rect.width - 16f)
					{
						curX = 4f;
						curY += GeneCreationDialogBase.GeneSize.y + 8f + 14f;
					}
					if (DrawGenepack(genepack, ref curX, curY, packWidth, containingRect))
					{
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
						this.selectedGenepacks.Remove(genepack);
						
						if (!xenotypeNameLocked)
						{
                            xenotypeName = GeneUtility.GenerateXenotypeNameFromGenes(SelectedGenes);
						}
						base.OnGenesChanged();
						break;
					}
				}
			}
			curY += GeneCreationDialogBase.GeneSize.y + 12f;
			if (Event.current.type == EventType.Layout)
			{
				sectionHeight = curY - startY;
			}
		}

		private void DrawDonorSection(Rect rect, ref float curY, ref float sectionHeight, Rect containingRect)
        {
			string label = "DonorGenes".Translate();
			float curX = rect.x;

			Rect labelRect = new Rect(10f, curY, rect.width - 16f - 10f, Text.LineHeight);
			Widgets.Label(labelRect, label);

			Text.Anchor = TextAnchor.UpperRight;
			GUI.color = ColoredText.SubtleGrayColor;
			Widgets.Label(labelRect, "ClickToRemove".Translate());
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;

			curY += Text.LineHeight + 3f;

			float startY = curY;
			Rect sectionRect = new Rect(0f, curY, rect.width, sectionHeight);
			//Widgets.DrawRectFast(sectionRect, Widgets.MenuSectionBGFillColor);
			curY += 4f;

			List<Genepack> genepacks = donorGenepacks;
			if (!genepacks.Any())
			{
				Text.Anchor = TextAnchor.MiddleCenter;
				GUI.color = ColoredText.SubtleGrayColor;
				Widgets.Label(sectionRect, "(" + "NoneLower".Translate() + ")");
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			else
            {
				for (int i = 0; i < genepacks.Count; i++)
				{
					Genepack genepack = genepacks[i];
					bool adding = true;
					if (selectedGenepacks.Contains(genepack))
					{
						adding = false;
					}
					float packWidth = 34f + GeneCreationDialogBase.GeneSize.x * (float)genepack.GeneSet.GenesListForReading.Count + 4f * (float)( genepack.GeneSet.GenesListForReading.Count + 2 );
					float rightPadding = 12f;
					int numRows = Mathf.Clamp(Mathf.CeilToInt(packWidth / (rect.width- rightPadding )), 1, 64);
					if (numRows > 1)
                    {
						packWidth = rect.width- rightPadding;
                    }
					float packHeight = ( GeneCreationDialogBase.GeneSize.y + 8f) * numRows;
					

					if (DrawDonorGenepack(genepack, ref curX, curY, packWidth, packHeight, adding, containingRect))
					{
						SoundDefOf.Tick_High.PlayOneShotOnCamera();
						if (adding)
                        {
							selectedGenepacks.Add(genepack);
						}
						else
                        {
							selectedGenepacks.Remove(genepack);
                        }
						OnGenesChanged();
						break;
					}
					curY += packHeight;
				}
			}
			//curY += GeneCreationDialogBase.GeneSize.y + 12f;
			if (Event.current.type == EventType.Layout)
			{
				sectionHeight = curY - startY;
			}
		}

		private bool DrawDonorGenepack(Genepack genepack, ref float curX, float curY, float packWidth, float packHeight, bool adding, Rect containingRect)
		{
			bool result = false;
			Rect rect = new Rect(curX, curY, packWidth, packHeight);
			if (genepack.GeneSet == null || genepack.GeneSet.GenesListForReading.NullOrEmpty())
			{
				Text.Anchor = TextAnchor.MiddleCenter;
				GUI.color = ColoredText.SubtleGrayColor;
				Widgets.Label(rect, "(" + "NoneLower".Translate() + ")");
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;
				return result;
			}
			if (!containingRect.Overlaps(rect))
			{
				curX = rect.xMax + 4f;
				return false;
			}
			if (selectedGenepacks.Contains(genepack))
            {
				// Draw an empty box instead if this genepack is currently being included

				Widgets.DrawHighlight(rect);

				Text.Anchor = TextAnchor.MiddleCenter;
				GUI.color = ColoredText.SubtleGrayColor;
				Widgets.Label(rect, "(" + "DonorXenotypeIncluded".Translate() + ")");
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;

				return false;
            }

			Widgets.DrawHighlight(rect);
			GUI.color = GeneCreationDialogBase.OutlineColorUnselected;
			Widgets.DrawBox(rect);
			GUI.color = Color.white;
			curX += 4f;
			float startX = curX;
			GeneUIUtility.DrawBiostats(genepack.GeneSet.ComplexityTotal, genepack.GeneSet.MetabolismTotal, genepack.GeneSet.ArchitesTotal, ref curX, curY, 4f);
			List<GeneDef> genesListForReading = genepack.GeneSet.GenesListForReading;
			for (int i = 0; i < genesListForReading.Count; i++)
			{
				GeneDef gene = genesListForReading[i];
				bool searchHighlighted = quickSearchWidget.filter.Active && matchingGenes.Contains(gene) && matchingGenepacks.Contains(genepack);
				bool overridden = leftChosenGroups.Any((GeneLeftChosenGroup x) => x.overriddenGenes.Contains(gene));
				Rect geneRect = new Rect(curX, curY + 4f, GeneCreationDialogBase.GeneSize.x, GeneCreationDialogBase.GeneSize.y);
				if (searchHighlighted)
				{
					Widgets.DrawStrongHighlight(geneRect.ExpandedBy(6f));
				}
				string extraTooltip = null;
				if (leftChosenGroups.Any((GeneLeftChosenGroup x) => x.leftChosen == gene))
				{
					extraTooltip = GroupInfo(leftChosenGroups.FirstOrDefault((GeneLeftChosenGroup x) => x.leftChosen == gene));
				}
				else if (cachedOverriddenGenes.Contains(gene))
				{
					extraTooltip = GroupInfo(leftChosenGroups.FirstOrDefault((GeneLeftChosenGroup x) => x.overriddenGenes.Contains(gene)));
				}
				else if (randomChosenGroups.ContainsKey(gene))
				{
					extraTooltip = ( "GeneWillBeRandomChosen".Translate() + ":\n" + randomChosenGroups[gene].Select((GeneDef x) => x.label).ToLineList("  - ", capitalizeItems: true) ).Colorize(ColoredText.TipSectionTitleColor);
				}
				GeneUIUtility.DrawGeneDef(genesListForReading[i], geneRect, GeneType.Xenogene, extraTooltip, doBackground: false, clickable: false, overridden);
				curX += GeneCreationDialogBase.GeneSize.x + 4f;
				if (curX + GeneCreationDialogBase.GeneSize.x > packWidth)
                {
					curX = startX;
					curY += GeneCreationDialogBase.GeneSize.y + 4f;
                }
			}
			bool mouseOver = Mouse.IsOver(rect);
			if ( mouseOver)
			{
				Widgets.DrawHighlight(rect);
			}
				
			if (!adding)
			{
				Widgets.DrawHighlight(rect);
			}
			
			if (Widgets.ButtonInvisible(rect))
			{
				result = true;
			}
			curX = Mathf.Max(curX, rect.xMax + 14f);
			return result;
		}

		private void DrawLibrarySection(Rect rect, string label, ref float curY, ref float sectionHeight, Rect containingRect)
		{
			float curX = 4f;
			Rect labelRect = new Rect(10f, curY, rect.width - 16f - 10f, Text.LineHeight);
			Widgets.Label(labelRect, label);
			curY += Text.LineHeight + 3f;
			float startY = curY;
			float startX = curX;
			Rect sectionRect = new Rect(0f, curY, rect.width, sectionHeight);
			Widgets.DrawRectFast(sectionRect, Widgets.MenuSectionBGFillColor);
			curY += 4f;

			List<Gene> endogenes = donorPawn.genes.Endogenes;

			if (!libraryGenepacks.Any() && !endogenes.Any())
			{
				Text.Anchor = TextAnchor.MiddleCenter;
				GUI.color = ColoredText.SubtleGrayColor;
				Widgets.Label(sectionRect, "(" + "NoneLower".Translate() + ")");
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			else
			{
				for (int i = 0; i < libraryGenepacks.Count; i++)
				{
					Genepack genepack = libraryGenepacks[i];
					// Don't draw the donor genepack in the library section
					if (donorGenepacks.Contains(genepack))
					{
						continue;
					}

						if (quickSearchWidget.filter.Active && ( !matchingGenepacks.Contains(genepack) || ( selectedGenepacks.Contains(genepack) ) ))
					{
						continue;
					}
					float packWidth = 34f + GeneCreationDialogBase.GeneSize.x * (float)genepack.GeneSet.GenesListForReading.Count + 4f * (float)( genepack.GeneSet.GenesListForReading.Count + 2 );
					if (curX + packWidth > rect.width - 16f)
					{
						curX = 4f;
						curY += GeneCreationDialogBase.GeneSize.y + 8f + 14f;
					}
					if (selectedGenepacks.Contains(genepack))
					{
						Widgets.DrawLightHighlight(new Rect(curX, curY, packWidth, GeneCreationDialogBase.GeneSize.y + 8f));
						curX += packWidth + 14f;
					}
					else if (DrawGenepack(genepack, ref curX, curY, packWidth, containingRect))
					{
						SoundDefOf.Tick_High.PlayOneShotOnCamera();
						selectedGenepacks.Add(genepack);
						if (!xenotypeNameLocked)
						{
							xenotypeName = GeneUtility.GenerateXenotypeNameFromGenes(SelectedGenes);
						}
						OnGenesChanged();
						break;
					}
				}
			}
			curY += GeneCreationDialogBase.GeneSize.y + 12f;
			if (Event.current.type == EventType.Layout)
			{
				sectionHeight = curY - startY;
			}
		}

		private bool DrawGeneasGenepack(GeneDef gene, ref float curX, float curY, float packWidth, GeneType geneType, bool locked, Rect containingRect, bool fromDonor=false, Thing infoCardFor=null)
        {
			bool result = false;
			Rect packRect = new Rect(curX, curY, packWidth, GeneCreationDialogBase.GeneSize.y + 8f);
			if (!containingRect.Overlaps(packRect))
			{
				//curX = rect.xMax + 4f;
			}
			if (geneType == GeneType.Xenogene)
			{
				Widgets.DrawHighlight(packRect);
				GUI.color = GeneCreationDialogBase.OutlineColorUnselected;
				Widgets.DrawBox(packRect);
				GUI.color = Color.white;
				curX += 4f;
				GeneUIUtility.DrawBiostats(gene.biostatCpx, gene.biostatMet, gene.biostatArc, ref curX, curY, 4f);
			}
			bool overridden = leftChosenGroups.Any((GeneLeftChosenGroup x) => x.overriddenGenes.Contains(gene));
			Rect geneRect = new Rect(curX, curY + 4f, GeneCreationDialogBase.GeneSize.x, GeneCreationDialogBase.GeneSize.y);

			string extraTooltip = "";
			if (cachedOverriddenGenes.Contains(gene))
			{
				extraTooltip = GroupInfo(leftChosenGroups.FirstOrDefault((GeneLeftChosenGroup x) => x.overriddenGenes.Contains(gene)));
			}
			else if (randomChosenGroups.ContainsKey(gene))
			{
				extraTooltip = ( "GeneWillBeRandomChosen".Translate() + ":\n" + randomChosenGroups[gene].Select((GeneDef x) => x.label).ToLineList("  - ", capitalizeItems: true) ).Colorize(ColoredText.TipSectionTitleColor);
			}

			string tooltip = geneType == GeneType.Xenogene ? "FromDonorXenotype".Translate() : "IsEndogeneTooltip".Translate();
			extraTooltip = extraTooltip + "\n\n" + tooltip;

			GeneUIUtility.DrawGeneDef(gene, geneRect, geneType, extraTooltip, doBackground: false, clickable: false, overridden);

			if (geneType == GeneType.Xenogene)
				Widgets.DrawRectFast(packRect, new Color(0.2f, 0.2f, 0.25f, 0.4f));

			if (infoCardFor != null)
				Widgets.InfoCardButton(packRect.xMax - 24f, packRect.y + 2f, infoCardFor);

			result = Widgets.ButtonInvisible(geneRect);
			curX += GeneCreationDialogBase.GeneSize.x + 4f;

			if (geneType == GeneType.Endogene)
				curX -= 300f; // Pack endogenes closer together

			curX = Mathf.Max(curX, packRect.xMax + 14f);

			if (Mouse.IsOver(packRect) && geneType == GeneType.Xenogene)
			{
				if (fromDonor)
					mouseOverAnyDonorGene = true;
				else
					Widgets.DrawHighlight(packRect);
			}
			if (highlightAllDonorGenes && fromDonor)
				Widgets.DrawHighlight(packRect);

			float lockY = geneType == GeneType.Xenogene ? packRect.yMax - 24f : packRect.yMin;
			Widgets.DrawTexturePart(new Rect(packRect.xMin, lockY, 24f, 24f), new Rect(0f, 0f, 1f, 1f), locked ? GeneCreationDialogBase.LockedTex : GeneCreationDialogBase.UnlockedTex);

			return result;
		}

		private bool DrawGenepack(Genepack genepack, ref float curX, float curY, float packWidth, Rect containingRect, bool fromDonor=false)
		{
			bool result = false;
			if (genepack.GeneSet == null || genepack.GeneSet.GenesListForReading.NullOrEmpty())
			{
				return result;
			}
			Rect rect = new Rect(curX, curY, packWidth, GeneCreationDialogBase.GeneSize.y + 8f);
			if (!containingRect.Overlaps(rect))
			{
				curX = rect.xMax + 4f;
				return false;
			}
			Widgets.DrawHighlight(rect);
			GUI.color = GeneCreationDialogBase.OutlineColorUnselected;
			Widgets.DrawBox(rect);
			GUI.color = Color.white;
			curX += 4f;
			GeneUIUtility.DrawBiostats(genepack.GeneSet.ComplexityTotal, genepack.GeneSet.MetabolismTotal, genepack.GeneSet.ArchitesTotal, ref curX, curY, 4f);
			List<GeneDef> genesListForReading = genepack.GeneSet.GenesListForReading;
			for (int i = 0; i < genesListForReading.Count; i++)
			{
				GeneDef gene = genesListForReading[i];
				bool searchHighlighted = quickSearchWidget.filter.Active && matchingGenes.Contains(gene) && matchingGenepacks.Contains(genepack);
				bool overridden = leftChosenGroups.Any((GeneLeftChosenGroup x) => x.overriddenGenes.Contains(gene));
				Rect geneRect = new Rect(curX, curY + 4f, GeneCreationDialogBase.GeneSize.x, GeneCreationDialogBase.GeneSize.y);
				if (searchHighlighted)
				{
					Widgets.DrawStrongHighlight(geneRect.ExpandedBy(6f));
				}
				string extraTooltip = null;
				if (leftChosenGroups.Any((GeneLeftChosenGroup x) => x.leftChosen == gene))
				{
					extraTooltip = GroupInfo(leftChosenGroups.FirstOrDefault((GeneLeftChosenGroup x) => x.leftChosen == gene));
				}
				else if (cachedOverriddenGenes.Contains(gene))
				{
					extraTooltip = GroupInfo(leftChosenGroups.FirstOrDefault((GeneLeftChosenGroup x) => x.overriddenGenes.Contains(gene)));
				}
				else if (randomChosenGroups.ContainsKey(gene))
				{
					extraTooltip = ( "GeneWillBeRandomChosen".Translate() + ":\n" + randomChosenGroups[gene].Select((GeneDef x) => x.label).ToLineList("  - ", capitalizeItems: true) ).Colorize(ColoredText.TipSectionTitleColor);
				}
				GeneUIUtility.DrawGeneDef(genesListForReading[i], geneRect, GeneType.Xenogene, extraTooltip, doBackground: false, clickable: false, overridden);
				curX += GeneCreationDialogBase.GeneSize.x + 4f;
			}
			Widgets.InfoCardButton(rect.xMax - 24f, rect.y + 2f, genepack);
			if (unpoweredGenepacks.Contains(genepack))
			{
				Widgets.DrawBoxSolid(rect, UnpoweredColor);
				TooltipHandler.TipRegion(rect, "GenepackUnusableGenebankUnpowered".Translate().Colorize(ColorLibrary.RedReadable));
			}
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
			}
			if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect) && Event.current.button == 1)
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				list.Add(new FloatMenuOption("EjectGenepackFromGeneBank".Translate(), delegate
				{
					CompGenepackContainer geneBankHoldingPack = cloneExtractor.GetGeneBankHoldingPack(genepack);
					if (geneBankHoldingPack != null)
					{
						ThingWithComps parent = geneBankHoldingPack.parent;
						if (geneBankHoldingPack.innerContainer.TryDrop(genepack, parent.def.hasInteractionCell ? parent.InteractionCell : parent.Position, parent.Map, ThingPlaceMode.Near, 1, out var _))
						{
							if (selectedGenepacks.Contains(genepack))
							{
								selectedGenepacks.Remove(genepack);
							}
							tmpGenes.Clear();
							libraryGenepacks.Clear();
							unpoweredGenepacks.Clear();
							matchingGenepacks.Clear();
							libraryGenepacks.AddRange(cloneExtractor.GetGenepacks(includePowered: true, includeUnpowered: true));
							unpoweredGenepacks.AddRange(cloneExtractor.GetGenepacks(includePowered: false, includeUnpowered: true));
							libraryGenepacks.SortGenepacks();
							unpoweredGenepacks.SortGenepacks();
							OnGenesChanged();
						}
					}
				}));
				Find.WindowStack.Add(new FloatMenu(list));
			}
			else if (Widgets.ButtonInvisible(rect))
			{
				result = true;
			}
			curX = Mathf.Max(curX, rect.xMax + 14f);
			return result;
		}
		static string GroupInfo(GeneLeftChosenGroup group)
		{
			if (group == null)
			{
				return null;
			}
			return ( "GeneOneActive".Translate() + ":\n  - " + group.leftChosen.LabelCap + " (" + "Active".Translate() + ")" + "\n" + group.overriddenGenes.Select((GeneDef x) => ( x.label + " (" + "Suppressed".Translate() + ")" ).Colorize(ColorLibrary.RedReadable)).ToLineList("  - ", capitalizeItems: true) ).Colorize(ColoredText.TipSectionTitleColor);
		}

		protected override void DrawSearchRect(Rect rect)
		{
			base.DrawSearchRect(rect);
			if (Widgets.ButtonText(new Rect(rect.xMax - GeneCreationDialogBase.ButSize.x, rect.y, GeneCreationDialogBase.ButSize.x, GeneCreationDialogBase.ButSize.y), "LoadXenogermTemplate".Translate()))
			{
				Find.WindowStack.Add(new Dialog_XenogermList_Load(delegate (CustomXenogerm xenogerm)
				{
					xenotypeName = xenogerm.name;
					iconDef = xenogerm.iconDef;
					IEnumerable<Genepack> collection = CustomXenogermUtility.GetMatchingGenepacks(xenogerm.genesets, libraryGenepacks);
					selectedGenepacks.Clear();
					selectedGenepacks.AddRange(collection);
					OnGenesChanged();
					IEnumerable<GeneSet> source = xenogerm.genesets.Where((GeneSet gp) => !selectedGenepacks.Any((Genepack g) => g.GeneSet.Matches(gp)));
					if (source.Any())
					{
						string text = null;
						int num = source.Count();
						if (num == 1)
						{
							text = "MissingGenepackForXenogerm".Translate(xenogerm.name.Named("NAME"));
							text = text + ": " + source.Select((GeneSet g) => g.Label).ToCommaList().CapitalizeFirst();
						}
						else
						{
							text = "MissingGenepacksForXenogerm".Translate(num.Named("COUNT"), xenogerm.name.Named("NAME"));
						}
						Messages.Message(text, null, MessageTypeDefOf.CautionInput, historical: false);
					}
				}));
			}
			if (Widgets.ButtonText(new Rect(rect.xMax - GeneCreationDialogBase.ButSize.x * 2f - 4f, rect.y, GeneCreationDialogBase.ButSize.x, GeneCreationDialogBase.ButSize.y), "SaveXenogermTemplate".Translate()))
			{
				AcceptanceReport acceptanceReport = CustomXenogermUtility.SaveXenogermTemplate(xenotypeName, iconDef, selectedGenepacks);
				if (!acceptanceReport.Reason.NullOrEmpty())
				{
					Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, historical: false);
				}
			}
		}

		protected override void DoBottomButtons(Rect rect)
		{
			base.DoBottomButtons(rect);
			int numTicks = Mathf.RoundToInt((float)Mathf.RoundToInt(GeneTuning.ComplexityToCreationHoursCurve.Evaluate(gcx) * 2500f) / cloneExtractor.GetStatValue(StatDefOf.AssemblySpeedFactor));
			Rect rect2 = new Rect(rect.center.x, rect.y, rect.width / 2f - GeneCreationDialogBase.ButSize.x - 10f, GeneCreationDialogBase.ButSize.y);
			TaggedString label;
			TaggedString taggedString;
			if (arc > 0 && !ResearchProjectDefOf.Archogenetics.IsFinished)
			{
				label = ( "MissingRequiredResearch".Translate() + ": " + ResearchProjectDefOf.Archogenetics.LabelCap ).Colorize(ColorLibrary.RedReadable);
				taggedString = "MustResearchProject".Translate(ResearchProjectDefOf.Archogenetics);
			}
			else
			{
				label = "RecombineDuration".Translate() + ": " + numTicks.ToStringTicksToPeriod();
				taggedString = "RecombineDurationDesc".Translate();
			}
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect2, label);
			Text.Anchor = TextAnchor.UpperLeft;
			if (Mouse.IsOver(rect2))
			{
				Widgets.DrawHighlight(rect2);
				TooltipHandler.TipRegion(rect2, taggedString);
			}
		}

		protected override bool CanAccept()
		{
			if (!base.CanAccept())
			{
				return false;
			}
			/*if (!selectedGenepacks.Any())
			{
				Messages.Message("MessageNoSelectedGenepacks".Translate(), null, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}*/
			if (arc > 0 && !ResearchProjectDefOf.Archogenetics.IsFinished && !CloningSettings.architeGenesFree)
			{
				Messages.Message("AssemblingRequiresResearch".Translate(ResearchProjectDefOf.Archogenetics), null, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			if (gcx > maxGCX)
			{
				Messages.Message("ComplexityTooHighToCreateXenogerm".Translate(gcx.Named("AMOUNT"), maxGCX.Named("MAX")), null, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			if (!ColonyHasEnoughArchites())
			{
				Messages.Message("NotEnoughArchites".Translate(), null, MessageTypeDefOf.RejectInput, historical: false);
				return false;
			}
			return true;
		}

		private bool ColonyHasEnoughArchites()
		{
			if (arc == 0 || cloneExtractor.MapHeld == null)
			{
				return true;
			}
			List<Thing> list = cloneExtractor.MapHeld.listerThings.ThingsOfDef(ThingDefOf.ArchiteCapsule);
			int num = 0;
			foreach (Thing item in list)
			{
				if (!item.Position.Fogged(cloneExtractor.MapHeld))
				{
					num += item.stackCount;
					if (num >= arc)
					{
						return true;
					}
				}
			}
			return false;
		}

		protected override void UpdateSearchResults()
		{
			quickSearchWidget.noResultsMatched = false;
			matchingGenepacks.Clear();
			matchingGenes.Clear();
			if (!quickSearchWidget.filter.Active)
			{
				return;
			}
			foreach (Genepack libraryGenepack in libraryGenepacks)
			{
				if (selectedGenepacks.Contains(libraryGenepack))
				{
					continue;
				}
				List<GeneDef> genesListForReading = libraryGenepack.GeneSet.GenesListForReading;
				for (int i = 0; i < genesListForReading.Count; i++)
				{
					if (quickSearchWidget.filter.Matches(genesListForReading[i].label))
					{
						matchingGenepacks.Add(libraryGenepack);
						matchingGenes.Add(genesListForReading[i]);
					}
				}
			}
			quickSearchWidget.noResultsMatched = !matchingGenepacks.Any();
		}

        public override void DoWindowContents(Rect rect)
        {
            base.DoWindowContents(rect);

			//HACK: This is a terrible way to do this in general, but it works I guess. A property would probably be better. Maybe a backing field that can override the getter
			if (!donorPawn.genes.UniqueXenotype && selectedGenepacks.Count == 1 && selectedGenepacks[0] == donorGenepacks[0] && xenotypeName == donorPawn.genes.XenotypeLabel) //HACK: This will break if I allow more than one donor genepack in the future
				UniqueXenotype = false;
			else
				UniqueXenotype = true;

			if (highlightAllDonorGenes)
				mouseOverAnyDonorGene = false;

			if (mouseOverAnyDonorGene)
				highlightAllDonorGenes = true;
			else
				highlightAllDonorGenes = false;
        }

        protected override void OnGenesChanged()
        {
            base.OnGenesChanged();

			if (CloningSettings.xenotypeGenesFree)
            {
				foreach (Genepack genepack in donorGenepacks)
				{
					this.gcx -= genepack.GeneSet.ComplexityTotal;
					this.gcx = Mathf.Max(this.gcx, 0); // Just to be safe, make sure it can't go negative for some reason
				}
            }
			if (CloningSettings.architeGenesFree)
            {
				foreach (Genepack genepack in donorGenepacks)
                {
					this.arc -= genepack.GeneSet.ArchitesTotal;
					this.arc = Mathf.Max(this.arc, 0);
                }
            }
        }
    }
}
