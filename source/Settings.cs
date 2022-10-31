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
    class Settings : ModSettings
    {
        public static bool cloningCooldown = true;
        public static bool inheritHair = true;

        public static bool doRandomMutations = true;
        public static float randomMutationChance = 0.3f;
        static readonly IntRange defaultNumMutations = new IntRange(1, 3);
        public static IntRange numMutations = new IntRange(1, 3);
        public static bool addMutationsAsXenogenes = false;
        public static bool cloneXenogenes = false;
        public static bool cloneArchiteGenes = false;
        static readonly IntRange defaultCloneExtractorRegrowingDurationDaysRange = new IntRange(7, 14);
        public static IntRange CloneExtractorRegrowingDurationDaysRange = new IntRange(7,14);

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
                    DoMainTab(inRect.BottomPartPixels(inRect.height - tabHeight - 24f));
                    break;
                case SettingsTab.Mutations:
                    DoMutationsTab(inRect.BottomPartPixels(inRect.height - tabHeight - 24f));
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

        void DoMainTab(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.ColumnWidth = inRect.width / 2f * 0.98f;
            listingStandard.CheckboxLabeled("Cloning_Settings_InheritHair".Translate(), ref Settings.inheritHair);
            listingStandard.CheckboxLabeled("Cloning_Settings_CloneXenogenes".Translate(), ref Settings.cloneXenogenes, "Cloning_Settings_CloneXenogenes_Tooltip".Translate());
            if (Settings.cloneXenogenes) listingStandard.CheckboxLabeled("Cloning_Settings_CloneArchiteGenes".Translate(), ref Settings.cloneArchiteGenes);
            else listingStandard.Gap(Text.CalcHeight("Cloning_Settings_CloneXenogenes".Translate(), listingStandard.ColumnWidth));

            listingStandard.NewColumn();

            #region Cooldown Settings

            listingStandard.CheckboxLabeled("Cloning_Settings_CloningCooldown".Translate(), ref Settings.cloningCooldown);
            if (Settings.cloningCooldown)
            {
                listingStandard.Label("Cloning_Settings_CooldownDays".Translate());
                listingStandard.IntRange(ref CloneExtractorRegrowingDurationDaysRange, 0, 60);
            }

            #endregion Cooldown Settings
            listingStandard.End();
        }

        void DoMutationsTab(Rect inRect)
        {
            float yMin = inRect.yMin;
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.ColumnWidth = inRect.width / 2f * 0.95f;

            listingStandard.CheckboxLabeled("Cloning_Settings_DoMutations".Translate(), ref Settings.doRandomMutations);
            listingStandard.Label("Cloning_Mutations_Description".Translate());
            if (Settings.doRandomMutations)
            {
                Settings.randomMutationChance = listingStandard.SliderLabeled(
                    "Cloning_Settings_MutationChance".Translate() + $": {Mathf.RoundToInt(Settings.randomMutationChance * 100)}%",
                    Settings.randomMutationChance,
                    0f,
                    1f
                );
                listingStandard.Label("Cloning_Settings_MaxMutations".Translate());
                listingStandard.IntRange(ref Settings.numMutations, 0, Settings.genesEligibleForMutation.Count);
                if (Settings.numMutations.max > Settings.genesEligibleForMutation.Count) Settings.numMutations.max = Settings.genesEligibleForMutation.Count;
                if (Settings.numMutations.min > Settings.genesEligibleForMutation.Count) Settings.numMutations.min = Settings.genesEligibleForMutation.Count;
                if (Settings.numMutations.max == 0) Settings.numMutations.max = Settings.numMutations.min;

                listingStandard.CheckboxLabeled("Cloning_Settings_MutationsXenogenes".Translate(), ref Settings.addMutationsAsXenogenes, "Cloning_Settings_MutationsXenogenes_Tooltip".Translate());

                #region Mutations Gene list
                float tmp = listingStandard.ColumnWidth;

                // Do a vertical gap between the columns
                listingStandard.NewColumn();
                listingStandard.ColumnWidth = 12f;

                listingStandard.NewColumn();
                listingStandard.ColumnWidth = tmp;

                // Begin scrollview for gene mutation settings
                Rect scrollRect = inRect.RightPartPixels(listingStandard.ColumnWidth);
                scrollRect.yMin = 50f;
                scrollRect.yMax = inRect.height;
                //scrollRect.yMax -= 50f;

                // Draw the header
                listingStandard.Label($"[{Settings.genesEligibleForMutation.Count}] " + "Cloning_Settings_GeneListHeader".Translate() + ":");

                // Draw buttons for manipulating the whole list at once.
                float selectButtonWidth = 60f;
                float selectButtonHeight = 24f;
                Rect selectButtonRect = new Rect(scrollRect.xMax - selectButtonWidth, scrollRect.yMin - selectButtonHeight, selectButtonWidth, selectButtonHeight);

                #region Selection Buttons
                if (Widgets.ButtonText(selectButtonRect, "Cloning_Settings_SelectNone".Translate()))
                {
                    Settings.genesEligibleForMutation.Clear();
                }
                selectButtonRect.position = new Vector2(selectButtonRect.x - ( selectButtonWidth + 6f ), selectButtonRect.y);
                if (Widgets.ButtonText(selectButtonRect, "Cloning_Settings_SelectAll".Translate()))
                {
                    foreach (GeneDef geneDef in GeneUtils.AllGenesCache.Values)
                    {
                        if (geneDef.defName == CloneDefs.Clone.defName) continue;
                        GeneUtils.SetEligible(geneDef.defName, true);
                    }
                }
                selectButtonRect.position = new Vector2(selectButtonRect.x - ( selectButtonWidth + 6f ), selectButtonRect.y);
                if (Widgets.ButtonText(selectButtonRect, "Cloning_Settings_Reset".Translate()))
                {
                    Settings.genesEligibleForMutation.Clear();
                    foreach (string geneDefault in GeneUtils.defaultGenesEligible.Keys)
                    {
                        Settings.genesEligibleForMutation.Add(geneDefault, GeneUtils.defaultGenesEligible[geneDefault]);
                    }
                }
                #endregion Selection Buttons

                // Draw the background for the sroll box
                Widgets.DrawBoxSolid(scrollRect, new Color(0.7f, 0.7f, 0.7f, 0.3f));

                UIUtility.MakeAndBeginScrollView(scrollRect, scrollHeight, ref scrollPos, out Listing_Standard scrollList);


                List<KeyValuePair<string, int>> genesList = genesEligibleForMutation.ToList();
                foreach (GeneCategoryDef geneCategory in GeneUtils.AllGeneCategoriesCache.Keys)
                {
                    float rowHeight = 86f;
                    float geneWidth = 72f;

                    Rect collapseBarRect = scrollList.GetRect(Text.LineHeight);
                    Widgets.DrawRectFast(collapseBarRect, new Color(0, 0, 0, 0.2f));
                    Rect labelRect = new Rect(collapseBarRect.x + 16f, collapseBarRect.y, collapseBarRect.width, collapseBarRect.height);
                    Widgets.Label(labelRect, geneCategory.LabelCap);

                    Rect rect2 = new Rect(0f, collapseBarRect.y, collapseBarRect.width, Text.LineHeight);
                    Rect position = new Rect(rect2.x, rect2.y + ( rect2.height - 18f ) / 2f, 18f, 18f);
                    GUI.DrawTexture(position, IsCategoryCollapsed(geneCategory) ? TexButton.Reveal : TexButton.Collapse);
                    if (Widgets.ButtonInvisible(collapseBarRect))
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
                    if (Mouse.IsOver(collapseBarRect))
                    {
                        Widgets.DrawHighlight(collapseBarRect);
                    }


                    RectRow row = new RectRow(4f, collapseBarRect.yMax, rowHeight, UIDirection.RightThenDown, scrollRect.width - 12f, 4f);
                    row.RowGapExtra = 36f;

                    if (!IsCategoryCollapsed(geneCategory))
                        DrawGenes(GeneUtils.AllGeneCategoriesCache[geneCategory], scrollList, row, geneWidth, rowHeight);
                    else
                        scrollList.GapLine(4f);
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

        void DrawGenes(List<string> genes, Listing_Standard scrollList, RectRow row, float geneWidth, float rowHeight)
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
                if (gene == CloneDefs.Clone.defName)
                {
                    continue;
                }

                // Draw one gene
                Rect buttonRect = row.GetRect(geneWidth, out bool newRow);
                if (newRow) scrollList.GetRect(rowHeight + row.CellGap + row.RowGapExtra); // Needed so that the scrollview listing_standard knows the correct height

                bool eligible = GeneUtils.IsEligible(gene); // Cache this so we don't force GeneUtils to do a .Contains several times for each gene.

                string extraTooltip = eligible ? "Enabled".Translate() : "Disabled".Translate();

                // Actually draw the gene, using vanilla's built-in static method for drawing genes from a def. This comes with the benefit of having the tooltip with all the gene's info
                GeneUIUtility.DrawGeneDef(GeneUtils.GeneNamed(gene), buttonRect, GeneType.Endogene, extraTooltip, true, false);

                // Draw things to indicate if this gene is in the eligible list or not
                if (eligible)
                {
                    Widgets.DrawHighlight(buttonRect);
                }
                else
                {
                    // Disabled (Dark overlay)
                    Widgets.DrawRectFast(buttonRect, new Color(0f, 0f, 0f, 0.3f));
                }

                // Draw a checkbox in the corner
                float checkboxSize = 16f;
                Rect checkboxRect = new Rect(buttonRect.xMin + 4f, buttonRect.yMin + 4f, checkboxSize, checkboxSize);
                Widgets.DrawTexturePart(checkboxRect, new Rect(0, 0, 1, 1), Widgets.GetCheckboxTexture(eligible));

                // Draw the weight slider and its label
                if (eligible)
                {
                    // Draw the weight slider
                    Rect sliderRect = new Rect(buttonRect.x, buttonRect.y + rowHeight + 24f, buttonRect.width, 24f);
                    genesEligibleForMutation[gene] = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, genesEligibleForMutation[gene], 0, 10));

                    // Draw the label for the slider
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
            scrollList.GetRect(rowHeight + row.CellGap + row.RowGapExtra); // Add another offset for the last row
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
    public class SettingsTabs<TEnum> where TEnum : Enum
    {
        
    }

    /// <summary>
    /// Utilities for UI, some of these are copied from sources online and are marked as such in comments.
    /// </summary>
    public static class UIUtility
    {
        static readonly Color defaultTabHighlightColor = new Color(0f, 0.5f, 0, 0.4f);
        private static readonly Texture2D TabAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/TabAtlas");

        /// <summary>
        /// Helper function to indent a Listing_Standard and adjust the column width to match
        /// </summary>
        public static void Indent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth -= amount;
            listing.Indent(amount);
        }
        /// <summary>
        /// Helper function to outdent a Listing_Standard and adjust the column width to match
        /// </summary>
        public static void Outdent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth += amount;
            listing.Outdent(amount);
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

            //float origColumnWidth = listingStandard.ColumnWidth;
            //listingStandard.ColumnWidth = width ?? listingStandard.ColumnWidth;

            //Rect rect = listingStandard.GetRect(height);
            Rect rect = inRect.TopPartPixels(height);
            float buttonWidth = ( rect.width / tabsList.Count) - ( gap * (tabsList.Count-1) );

            Rect buttonRect = new Rect(rect.x, rect.y, buttonWidth, height);
            for (int i = 0; i < tabsList.Count; i++)
            {
                TabSwitchButton(buttonRect, tabsList[i].Value, ref currentTab, tabsList[i].Key, highlightColor);
                buttonRect.x += buttonWidth + gap;
            }
            //listingStandard.ColumnWidth = origColumnWidth;
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

