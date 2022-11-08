using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace Dark.Cloning
{
    class CloningSettings : ModSettings
    {
        public static bool cloningCooldown = true;
        public static bool inheritHair = true;
        public static bool cloneXenogenes = false;
        public static bool cloneArchiteGenes = false;
        public static bool cloneTraits = false;
        static readonly IntRange defaultCloneExtractorRegrowingDurationDaysRange = new IntRange(7, 14);
        public static IntRange CloneExtractorRegrowingDurationDaysRange = new IntRange(7,14);

        public static bool doRandomMutations = true;
        public static float randomMutationChance = 0.3f;
        static readonly IntRange defaultNumMutations = new IntRange(1, 3);
        public static IntRange numMutations = new IntRange(1, 3);
        public static bool addMutationsAsXenogenes = false;
        public static Dictionary<string, int> genesEligibleForMutation = GeneUtils.defaultGenesEligible;


        // Internal UI values
        float scrollHeight = 9999f;
        Vector2 scrollPos;
        Dictionary<GeneCategoryDef, bool> categoryCollapsedStates = new Dictionary<GeneCategoryDef, bool>();
        float tabHeight = 32f;
        public enum SettingsTab
        {
            Main,
            Mutations
        }
        SettingsTab currentTab;

        public void DoWindowContents(Rect inRect)
        {
            Rect tabsRect = new Rect(inRect.x, inRect.y + 24f, inRect.width, inRect.height);
            DoTabs(tabsRect);

            switch (currentTab)
            {
                case SettingsTab.Main:
                    DoMainTabContents(inRect.BottomPartPixels(inRect.height - tabHeight - 24f));
                    break;
                case SettingsTab.Mutations:
                    DoMutationsTabContents(inRect.BottomPartPixels(inRect.height - tabHeight - 24f));
                    break;
                default:
                    break;
            }
        }

        public void DoTabs(Rect inRect)
        {
            List<KeyValuePair<SettingsTab, string>> tabsList = new List<KeyValuePair<SettingsTab, string>> {
                new KeyValuePair<SettingsTab, string>(SettingsTab.Main, "Cloning_Settings_Tab_Main".Translate() ),
                new KeyValuePair<SettingsTab, string>(SettingsTab.Mutations, "Cloning_Settings_Tab_Mutations".Translate())
            };
            List<TabRecord> tabs = new List<TabRecord>();

            foreach (KeyValuePair<SettingsTab, string> tab in tabsList)
            {
                tabs.Add(new TabRecord(tab.Value, delegate { currentTab = tab.Key; }, Convert.ToInt32(currentTab) == Convert.ToInt32(tab.Key)));
            }
            TabDrawer.DrawTabs(inRect, tabs, inRect.width/tabs.Count);
        }

        void DoMainTabContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.ColumnWidth = inRect.width / 2f * 0.98f;
            listingStandard.CheckboxLabeled("Cloning_Settings_InheritHair".Translate(), ref CloningSettings.inheritHair);
            listingStandard.CheckboxLabeled("Cloning_Settings_CloneXenogenes".Translate(), ref CloningSettings.cloneXenogenes, "Cloning_Settings_CloneXenogenes_Tooltip".Translate());
            if (CloningSettings.cloneXenogenes)
            {
                listingStandard.DoIndent();
                listingStandard.CheckboxLabeled("Cloning_Settings_CloneArchiteGenes".Translate(), ref CloningSettings.cloneArchiteGenes);
                listingStandard.DoOutdent();
            }
            else
                listingStandard.Gap(Text.CalcHeight("Cloning_Settings_CloneXenogenes".Translate(), listingStandard.ColumnWidth));
            //listingStandard.CheckboxLabeled("Cloning_Settings_CloneTraits".Translate(), ref Settings.cloneTraits);

            listingStandard.NewColumn();

            #region Cooldown Settings

            listingStandard.CheckboxLabeled("Cloning_Settings_CloningCooldown".Translate(), ref CloningSettings.cloningCooldown);
            if (CloningSettings.cloningCooldown)
            {
                listingStandard.Label("Cloning_Settings_CooldownDays".Translate());
                listingStandard.IntRange(ref CloneExtractorRegrowingDurationDaysRange, 0, 60);
            }

            #endregion Cooldown Settings
            listingStandard.End();
        }

        void DoMutationsTabContents(Rect inRect)
        {
            float yMin = inRect.yMin;
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.ColumnWidth = inRect.width / 2f * 0.95f;

            listingStandard.CheckboxLabeled("Cloning_Settings_DoMutations".Translate(), ref CloningSettings.doRandomMutations);
            listingStandard.Label("Cloning_Mutations_Description".Translate());
            if (CloningSettings.doRandomMutations)
            {
                listingStandard.CheckboxLabeled("Cloning_Settings_MutationsXenogenes".Translate(), ref CloningSettings.addMutationsAsXenogenes, "Cloning_Settings_MutationsXenogenes_Tooltip".Translate());

                CloningSettings.randomMutationChance = listingStandard.SliderLabeled(
                    "Cloning_Settings_MutationChance".Translate() + $": {Mathf.RoundToInt(CloningSettings.randomMutationChance * 100)}%",
                    CloningSettings.randomMutationChance,
                    0f,
                    1f
                );
                listingStandard.Label("Cloning_Settings_MaxMutations".Translate());
                listingStandard.IntRange(ref CloningSettings.numMutations, 0, CloningSettings.genesEligibleForMutation.Count);
                if (CloningSettings.numMutations.max > CloningSettings.genesEligibleForMutation.Count) CloningSettings.numMutations.max = CloningSettings.genesEligibleForMutation.Count;
                if (CloningSettings.numMutations.min > CloningSettings.genesEligibleForMutation.Count) CloningSettings.numMutations.min = CloningSettings.genesEligibleForMutation.Count;
                if (CloningSettings.numMutations.max == 0) CloningSettings.numMutations.max = CloningSettings.numMutations.min;

                listingStandard.DoBlankColumn(12f);

                #region Mutations Gene list

                // Begin scrollview for gene mutation settings
                Rect scrollRect = inRect.RightPartPixels(listingStandard.ColumnWidth);
                scrollRect.yMin = 50f;
                scrollRect.yMax = inRect.height;

                // Draw the header
                listingStandard.Label($"[{CloningSettings.genesEligibleForMutation.Count}] " + "Cloning_Settings_GeneListHeader".Translate() + ":");

                // Draw buttons for collapsing categories
                float collapseButtonsHeight = 24f;
                Rect collapseButtonsRect = new Rect(scrollRect.xMin, scrollRect.yMin - collapseButtonsHeight, scrollRect.width, collapseButtonsHeight).LeftHalf();
                if (Widgets.ButtonText(collapseButtonsRect.RightHalf(), "Cloning_Settings_CollapseAll".Translate()))
                {
                    ToggleCollapseOnAllCategories(true);
                }
                if (Widgets.ButtonText(collapseButtonsRect.LeftHalf(), "Cloning_Settings_ExpandAll".Translate()))
                {
                    ToggleCollapseOnAllCategories(false);
                }

                // Draw buttons for manipulating the whole list at once.
                //TODO: Move these elsewhere in the menu
                //TODO: Add preset save/load buttons
                float selectButtonWidth = 60f;
                float selectButtonHeight = 24f;
                Rect selectButtonRect = new Rect(scrollRect.xMax - selectButtonWidth, scrollRect.yMin - selectButtonHeight, selectButtonWidth, selectButtonHeight);

                #region Selection Buttons
                if (Widgets.ButtonText(selectButtonRect, "Cloning_Settings_SelectNone".Translate()))
                {
                    CloningSettings.genesEligibleForMutation.Clear();
                }
                selectButtonRect.position = new Vector2(selectButtonRect.x - ( selectButtonWidth + 6f ), selectButtonRect.y);
                if (Widgets.ButtonText(selectButtonRect, "Cloning_Settings_SelectAll".Translate()))
                {
                    foreach (GeneDef geneDef in GeneUtils.AllGenesCache.Values)
                    {
                        if (geneDef.defName == CloneDefOf.Clone.defName) continue;
                        GeneUtils.SetEligible(geneDef.defName, true);
                    }
                }
                selectButtonRect.position = new Vector2(selectButtonRect.x - ( selectButtonWidth + 6f ), selectButtonRect.y);
                if (Widgets.ButtonText(selectButtonRect, "Cloning_Settings_Reset".Translate()))
                {
                    CloningSettings.genesEligibleForMutation.Clear();
                    foreach (string geneDefault in GeneUtils.defaultGenesEligible.Keys)
                    {
                        CloningSettings.genesEligibleForMutation.Add(geneDefault, GeneUtils.defaultGenesEligible[geneDefault]);
                    }
                }
                #endregion Selection Buttons

                // Draw the background for the sroll box
                Widgets.DrawBoxSolid(scrollRect, new Color(0.7f, 0.7f, 0.7f, 0.3f));

                UIUtility.MakeAndBeginScrollView(scrollRect, scrollHeight, ref scrollPos, out Listing_Standard scrollList);


                List<KeyValuePair<string, int>> genesList = genesEligibleForMutation.ToList();
                foreach (GeneCategoryDef geneCategory in GeneUtils.AllGeneCategoriesCache.Keys)
                {
                    DrawGeneCategory(geneCategory, scrollList, scrollRect);
                }

                UIUtility.EndScrollView(scrollList, ref scrollHeight);
                #endregion Mutations Gene list
            }
            listingStandard.End();
        }

        bool IsCategoryCollapsed(GeneCategoryDef category)
        {
            if (!categoryCollapsedStates.ContainsKey(category))
            {
                categoryCollapsedStates.Add(category, true);
            }
            return categoryCollapsedStates[category];
        }

        void ToggleCollapseOnAllCategories(bool newCollapsed)
        {
            foreach (GeneCategoryDef category in GeneUtils.AllGeneCategoriesCache.Keys)
            {
                categoryCollapsedStates[category] = newCollapsed;
            }
        }

        void SetEnableForCategory(GeneCategoryDef category, bool newEnabled)
        {
            foreach (string gene in GeneUtils.AllGeneCategoriesCache[category])
            {
                GeneUtils.SetEligible(gene, newEnabled);
            }
        }

        void DrawGene(string gene, Listing_Standard scrollList, RectRow row, Rect scrollRect, float geneWidth, float rowHeight)
        {
            Rect buttonRect = row.GetRect(geneWidth, out bool newRow);
            if (newRow) scrollList.GetRect(rowHeight + row.CellGap + row.RowGapExtra); // Needed so that the scrollview listing_standard knows the correct height

            // Cull offscreen genes
            Rect visibleRect = new Rect(scrollPos.x, scrollPos.y, scrollRect.width, scrollRect.height);
            if ( !( visibleRect.Contains(new Vector2(buttonRect.xMin, buttonRect.yMin)) || visibleRect.Contains(new Vector2(buttonRect.xMax, buttonRect.yMax)) ) )
            {
                return;
            }

            bool eligible = GeneUtils.IsEligible(gene); // Cache this so we don't force GeneUtils to do a .Contains several times for each gene.

            // Actually draw the gene, using vanilla's built-in static method for drawing genes from a def. This comes with the benefit of having the tooltip with all the gene's info
            GeneUIUtility.DrawGeneDef(GeneUtils.GeneNamed(gene), buttonRect, GeneType.Endogene, eligible ? "Enabled".Translate() : "Disabled".Translate(), true, false);

            if (eligible)
                Widgets.DrawHighlight(buttonRect);
            else
                Widgets.DrawRectFast(buttonRect, new Color(0f, 0f, 0f, 0.3f));

            // Draw a checkbox in the corner
            float checkboxSize = 16f;
            Rect checkboxRect = new Rect(buttonRect.xMin + 4f, buttonRect.yMin + 4f, checkboxSize, checkboxSize);
            Widgets.DrawTexturePart(checkboxRect, new Rect(0, 0, 1, 1), Widgets.GetCheckboxTexture(eligible));

            // Draw the weight slider and its label
            if (eligible)
            {
                Rect sliderRect = new Rect(buttonRect.x, buttonRect.y + rowHeight + 24f, buttonRect.width, 24f);
                genesEligibleForMutation[gene] = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, genesEligibleForMutation[gene], 0, 10));

                Rect sliderLabelRect = new Rect(buttonRect.x + 4f, buttonRect.y + rowHeight, buttonRect.width, 24f);
                Widgets.Label(sliderLabelRect, "Cloning_Settings_MutationGeneWeight".Translate() + ": " + genesEligibleForMutation[gene]);
            }

            // Lastly, handle clicking on this gene, by flipflopping its eligibility status
            if (Widgets.ButtonInvisible(buttonRect))
            {
                GeneUtils.SetEligible(gene, !eligible);
                eligible = !eligible;
            }
        }

        void DrawGenes(List<string> genes, Listing_Standard scrollList, RectRow row, Rect scrollRect, float geneWidth, float rowHeight)
        {
            foreach (string gene in genes)
            {
                // If the current gene isn't loaded (such as if it's from another mod that isn't currently running), skip it.
                if (!GeneUtils.IsCached(gene))
                {
                    Log.Warning($"Could not find def for gene {gene}. Is it from a mod that is no longer loaded?");
                    continue;
                }

                // Skip the Clone gene added by this mod. It wouldn't make any sense for a clone to randomly gain the Clone gene
                if (gene == CloneDefOf.Clone.defName)
                {
                    continue;
                }

                // Draw one gene
                DrawGene(gene, scrollList, row, scrollRect, geneWidth, rowHeight);
            }
            scrollList.GetRect(rowHeight + row.CellGap + row.RowGapExtra); // Add another offset for the last row
        }

        Rect DrawCategoryCollapsibleHeader(GeneCategoryDef geneCategory, Listing_Standard scrollList)
        {
            float clickablePercent = 0.7f;
            Rect collapseBarRect = scrollList.GetRect(Text.LineHeight);
            Widgets.DrawRectFast(collapseBarRect, new Color(0, 0, 0, 0.2f));
            Rect labelRect = new Rect(collapseBarRect.x + 16f, collapseBarRect.y, collapseBarRect.width, collapseBarRect.height);
            Widgets.Label(labelRect, geneCategory.LabelCap);

            Rect rect2 = new Rect(0f, collapseBarRect.y, collapseBarRect.width, Text.LineHeight);
            Rect position = new Rect(rect2.x, rect2.y + ( rect2.height - 18f ) / 2f, 18f, 18f);
            GUI.DrawTexture(position, IsCategoryCollapsed(geneCategory) ? TexButton.Reveal : TexButton.Collapse);
            if (Widgets.ButtonInvisible(collapseBarRect.LeftPart(clickablePercent)))
            {
                categoryCollapsedStates[geneCategory] = !IsCategoryCollapsed(geneCategory);
                if (categoryCollapsedStates[geneCategory])
                {
                    SoundDefOf.TabClose.PlayOneShotOnCamera();
                }
                else
                {
                    SoundDefOf.TabOpen.PlayOneShotOnCamera();
                }
            }
            if (Mouse.IsOver(collapseBarRect.LeftPart(clickablePercent)))
            {
                Widgets.DrawHighlight(collapseBarRect);
            }

            // Draw buttons for enabling/disabling the whole category at once
            Rect enableButtonsRect = collapseBarRect.RightPart(1f - clickablePercent);
            if (Widgets.ButtonText(enableButtonsRect.LeftHalf(), "Cloning_Settings_DisableAll".Translate()))
            {
                SetEnableForCategory(geneCategory, false);
            }
            if (Widgets.ButtonText(enableButtonsRect.RightHalf(), "Cloning_Settings_EnableAll".Translate()))
            {
                SetEnableForCategory(geneCategory, true);
            }

            return collapseBarRect;
        }

        void DrawGeneCategory(GeneCategoryDef geneCategory, Listing_Standard scrollList, Rect scrollRect)
        {
            float rowHeight = 86f;
            float geneWidth = 72f;

            Rect collapseBarRect = DrawCategoryCollapsibleHeader(geneCategory, scrollList);

            RectRow row = new RectRow(4f, collapseBarRect.yMax, rowHeight, UIDirection.RightThenDown, scrollRect.width - 12f, 4f);
            row.RowGapExtra = 36f;

            if (!IsCategoryCollapsed(geneCategory))
                DrawGenes(GeneUtils.AllGeneCategoriesCache[geneCategory], scrollList, row, scrollRect, geneWidth, rowHeight);
            else
                scrollList.GapLine(4f);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref cloningCooldown, "cloningCooldown", true);
            Scribe_Values.Look(ref inheritHair, "inheritHair", true);
            Scribe_Values.Look(ref CloneExtractorRegrowingDurationDaysRange, "CloneExtractorRegrowingDurationDaysRange", defaultCloneExtractorRegrowingDurationDaysRange);
            Scribe_Values.Look(ref doRandomMutations, "doRandomMutations", true);
            Scribe_Values.Look(ref randomMutationChance, "randomMutationChance", 0.3f);
            Scribe_Values.Look(ref numMutations, "numMutations", defaultNumMutations);
            Scribe_Values.Look(ref addMutationsAsXenogenes, "addMutationsAsXenogenes", false);
            Scribe_Values.Look(ref cloneXenogenes, "cloneXenogenes", false);
            Scribe_Values.Look(ref cloneArchiteGenes, "cloneArchiteGenes", false);
            Scribe_Values.Look(ref cloneTraits, "cloneTraits", false);
            Scribe_Collections.Look(ref genesEligibleForMutation, "genesEligibleForMutation", LookMode.Value);

            // Check if the dictionary loaded was empty, and load the default instead
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (genesEligibleForMutation == null || genesEligibleForMutation.Count <= 0)
                {
                    genesEligibleForMutation = GeneUtils.defaultGenesEligible;
                }
            }


            base.ExposeData();
        }
    }

    #region UI Utilities
    /// <summary>
    /// Utilities for UI, some of these are copied from sources online and are marked as such in comments.
    /// </summary>
    public static class UIUtility
    {
        /// <summary>
        /// Helper function to indent a Listing_Standard and adjust the column width to match
        /// </summary>
        public static void DoIndent(this Listing_Standard listing, float amount=12f)
        {
            listing.ColumnWidth -= amount;
            listing.Indent(amount);
        }
        /// <summary>
        /// Helper function to outdent a Listing_Standard and adjust the column width to match
        /// </summary>
        public static void DoOutdent(this Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth += amount;
            listing.Outdent(amount);
        }

        public static void DoBlankColumn(this Listing_Standard listing, float width = 12f)
        {
            float tmp = listing.ColumnWidth;

            // Do a vertical gap between the columns
            listing.NewColumn();
            listing.ColumnWidth = width;

            listing.NewColumn();
            listing.ColumnWidth = tmp;
        }
        /// <summary>
        /// Draws one button used for switching tabs based on an enum value
        /// </summary>
        /// <param name="currentTab">A ref value keeping track of which tab is current</param>
        /// <param name="tab">The enum value that this button represents</param>
        /// <param name="highlightColor">Optional color to overlay when this button's tab is the current tab</param>
        public static void TabSwitchButton<TEnum>(Rect rect, string label, ref TEnum currentTab, TEnum tab, Color? highlightColor = null) where TEnum : Enum
        {
            bool clicked = false;
            TabRecord tabRecord = new TabRecord(label, () => { clicked = true; }, Convert.ToInt32(currentTab) == Convert.ToInt32(tab));
            tabRecord.Draw(rect);
            if (clicked) currentTab = tab;
        }
        /// <summary>
        /// Draws a horizontal row of buttons for switching tabs
        /// </summary>
        /// <param name="currentTab">A ref value keeping track of which tab is current</param>
        /// <param name="tabsList">A list of KeyValuePairs which has enum keys and strings representing the tab button's text</param>
        /// <param name="width">Optional width for the tabs to take up. If not supplied, will take up the entire width of the listing_standard</param>
        /// <param name="gap">Gap between tab buttons</param>
        /// <param name="highlightColor">Optional color to overlay when this button's tab is the current tab</param>
        public static void DoTabs<TEnum>(Rect inRect, ref TEnum currentTab, List<KeyValuePair<TEnum, string>> tabsList, float height = 24f, float gap = 8f, Color? highlightColor = null)
            where TEnum : Enum
        {
            List<TabRecord> tabs = new List<TabRecord>();
            TEnum curTab = currentTab;
            //for (int i = 0; i < tabsList.Count; i++)
            foreach (KeyValuePair<TEnum, string> tab in tabsList)
            {
                tabs.Add(new TabRecord(tab.Value, delegate { curTab = tab.Key; }, Convert.ToInt32(currentTab) == Convert.ToInt32(tab.Key)));
            }
            currentTab = curTab;
            TabDrawer.DrawTabs(inRect, tabs, 420f);
            return;
        }

        #region NC ScrollView
        public const float DefaultSliderWidth = 16f;   // will be subtracted from width for inner windows in scroll views

        // Copied from https://gitlab.com/nightcorp/nightmarecore/-/blob/master/Source/Utilities/UIUtility.cs#L29
        public static Rect CreateInnerScrollRect(Rect outerRect, float height, float sliderWidth = DefaultSliderWidth)
        {
            return new Rect
            (
                0,
                0,
                outerRect.width - sliderWidth,
                height
            );
        }
        // Copied from https://gitlab.com/nightcorp/nightmarecore/-/blob/master/Source/Utilities/UIUtility.cs#L29
        public static void MakeAndBeginScrollView(Rect inRect, float requiredHeight, ref Vector2 scrollPosition, out Listing_Standard list)
        {
            MakeAndBeginScrollView(inRect, requiredHeight, ref scrollPosition, out Rect innerRect);
            list = new Listing_Standard()
            {
                ColumnWidth = innerRect.width,
                maxOneColumn = true
            };
            list.Begin(innerRect);
        }
        // Copied from https://gitlab.com/nightcorp/nightmarecore/-/blob/master/Source/Utilities/UIUtility.cs#L29
        public static void MakeAndBeginScrollView(Rect inRect, float requiredHeight, ref Vector2 scrollPosition, out Rect outRect)
        {
            outRect = CreateInnerScrollRect(inRect, requiredHeight);
            Widgets.BeginScrollView(inRect, ref scrollPosition, outRect);
        }

        // Copied from https://gitlab.com/nightcorp/nightmarecore/-/blob/master/Source/Utilities/UIUtility.cs#L29
        public static void EndScrollView(this Listing_Standard list, ref float requiredHeight)
        {
            requiredHeight = list.CurHeight;
            list.End();
            Widgets.EndScrollView();
        }
        #endregion NC ScrollView
    }

    /// <summary>
    /// Based on Verse.WidgetRow, but rewritten to be more general,
    /// only returning Rects and not actually drawing anything itself. <br />
    /// Encapsulated class that could be used in other mods to draw a wrapping grid of anything using Rects. <br />
    /// Designed to be used like Listing_Standard.GetRect(), but horizontally instead.
    /// </summary>
    public class RectRow
    {
        private float startX;
        private float curX;
        private float curY;
        private float maxWidth = 99999f;
        private float gap;
        private float rowGapExtra = 0;
        private float rowHeight;
        private UIDirection growDirection = UIDirection.RightThenDown;
        public const float DefaultGap = 4f;
        private const float DefaultMaxWidth = 99999f;

        public float FinalX => this.curX;

        public float FinalY => this.curY;

        public float CellGap
        {
            get => this.gap;
            set => this.gap = value;
        }

        public float RowGapExtra
        {
            get => this.rowGapExtra;
            set => this.rowGapExtra = value;
        }

        public float RowHeight 
        { 
            get => this.RowHeight;
            set => this.RowHeight = value; 
        }

        public RectRow(float x, float y, float rowHeight, UIDirection growDirection = UIDirection.RightThenDown, float maxWidth = 99999f, float gap = 4f) => this.Init(x, y, rowHeight, growDirection, maxWidth, gap);

        #region Copied From Vanilla
        public void Init(float x, float y, float rowHeight, UIDirection growDirection = UIDirection.RightThenUp, float maxWidth = 99999f, float gap = 4f)
        {
            this.growDirection = growDirection;
            this.startX = x;
            this.curX = x;
            this.curY = y;
            this.maxWidth = maxWidth;
            this.gap = gap;
            this.rowHeight = rowHeight;
        }

        /// <summary>
        /// Returns the x coordinate of the left side of the next element with width elementWidth. Takes into account the growDirection .
        /// </summary>
        /// <param name="elementWidth"></param>
        /// <returns></returns>
        private float LeftX(float elementWidth) => this.growDirection == UIDirection.RightThenUp || this.growDirection == UIDirection.RightThenDown ? this.curX : this.curX - elementWidth;

        private void IncrementPosition(float amount)
        {
            if (this.growDirection == UIDirection.RightThenUp || this.growDirection == UIDirection.RightThenDown)
                this.curX += amount;
            else
                this.curX -= amount;
            if ((double)Mathf.Abs(this.curX - this.startX) <= (double)this.maxWidth)
                return;
            this.IncrementY();
        }

        private void IncrementY()
        {
            if (this.growDirection == UIDirection.RightThenUp || this.growDirection == UIDirection.LeftThenUp)
                this.curY -= this.rowHeight + this.gap + this.rowGapExtra;
            else
                this.curY += this.rowHeight + this.gap + this.rowGapExtra;
            this.curX = this.startX;
        }

        // Modified slightly from vanilla to return a bool representing if it did wrap to a new row
        private bool IncrementYIfWillExceedMaxWidth(float width)
        {
            if ((double)Mathf.Abs(this.curX - this.startX) + (double)Mathf.Abs(width) <= (double)this.maxWidth)
                return false;
            this.IncrementY();
            return true;
        }

        public void Gap(float width)
        {
            if ((double)this.curX == (double)this.startX)
                return;
            this.IncrementPosition(width);
        }
        #endregion Copied From Vanilla

        public Rect GetRect(float width, out bool newRow)
        {
            Vector2 rectSize = new Vector2(width, this.rowHeight);

            newRow = this.IncrementYIfWillExceedMaxWidth(rectSize.x);

            Rect rect = new Rect(this.LeftX(rectSize.x), this.curY, rectSize.x, rectSize.y);
            this.IncrementPosition(rect.width + this.gap);

            return rect;
        }
    }
    #endregion UI Utilities
}

