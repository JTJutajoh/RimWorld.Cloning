using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace Dark.Cloning
{
    class Settings : ModSettings
    {
        public static bool cloningCooldown = true;
        public static int cloningCooldownDays = 10;
        private const int defaultCooldownDays = 10;

        public static bool doRandomMutations = true;
        public static float randomMutationChance = 0.3f;
        public static int maxMutations = 3;

        public static Dictionary<string, int> genesEligibleForMutation = GeneUtils.defaultGenesEligible;


        float scrollHeight = 9999f;
        Vector2 scrollPos;


        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();

            listingStandard.Begin(inRect);

            #region Cooldown Settings
            listingStandard.CheckboxLabeled("Cloning_Settings_CloningCooldown".Translate(), ref Settings.cloningCooldown, "Cloning_Cooldown_Description".Translate());
            listingStandard.Label("Cloning_Cooldown_Description".Translate());
            UIUtility.DoIndent(listingStandard);
            if (Settings.cloningCooldown)
            {
                Settings.cloningCooldownDays = (int)listingStandard.SliderLabeled(
                    "Cloning_Settings_CooldownDays".Translate() + ": " + Settings.cloningCooldownDays + " Days",
                    Settings.cloningCooldownDays,
                    0,
                    240,
                    tooltip: "Cloning_Settings_CooldownDays_Tooltip".Translate() + ": " + defaultCooldownDays
                );
            }
            UIUtility.DoOutdent(listingStandard);
            #endregion Cooldown Settings

            listingStandard.GapLine();

            #region Mutation Settings
            listingStandard.CheckboxLabeled("Cloning_Settings_DoMutations".Translate(), ref Settings.doRandomMutations);
            listingStandard.Label("Cloning_Mutations_Description".Translate());
            UIUtility.DoIndent(listingStandard);
            if (Settings.doRandomMutations)
            {
                Settings.randomMutationChance = listingStandard.SliderLabeled(
                    "Cloning_Settings_MutationChance".Translate() + $": {Mathf.RoundToInt(Settings.randomMutationChance * 100)}%",
                    Settings.randomMutationChance,
                    0f,
                    1f
                );
                Settings.maxMutations = Mathf.RoundToInt(listingStandard.SliderLabeled(
                    "Cloning_Settings_MaxMutations".Translate() + $": {Settings.maxMutations}",
                    Settings.maxMutations,
                    0f,
                    10f
                ));

                // Begin scrollview for gene mutation settings
                Rect scrollRect;

                scrollRect = inRect.BottomPart(0.6f);
                //scrollRect = scrollRect.RightPart(0.6f);
                scrollRect.yMax -= 50f;

                // Draw the header
                float listHeaderSize = 20f;
                Rect listHeaderRect = new Rect();
                listHeaderRect.xMax = scrollRect.xMax;
                listHeaderRect.yMin = scrollRect.yMin - listHeaderSize;
                listHeaderRect.yMax = listHeaderRect.yMin + listHeaderSize;
                //listHeaderRect.xMin -= scrollRect.xMin + 32f;
                Widgets.Label(listHeaderRect, $"({Settings.genesEligibleForMutation.Count})" + "Cloning_Settings_GeneListHeader".Translate() + ":");

                // Draw buttons for manipulating the whole list at once.
                float selectButtonWidth = 60f;
                float selectButtonHeight = 24f;
                Rect selectButtonRect = new Rect(scrollRect.xMax-selectButtonWidth,scrollRect.yMin- selectButtonHeight, selectButtonWidth, selectButtonHeight);

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
                    //Settings.genesEligibleForMutation = GeneUtils.defaultGenesEligible;
                }


                // Draw the background for the sroll box
                Widgets.DrawBoxSolid(scrollRect, new Color(0.7f, 0.7f, 0.7f, 0.3f));

                // Whatever you do, DO NOT SET THIS TO 2f!!!!!! For god knows what reason, that causes it to fail at rendering everything!
                // FOR THE LOVE OF GOD don't do it. And DON'T try to figure out why, just leave it be. Live your life in blissful ignorance.
                scrollRect = scrollRect.ContractedBy(8f); 


                UIUtility.MakeAndBeginScrollView(scrollRect, scrollHeight, ref scrollPos, out Listing_Standard scrollList);
                scrollRect.xMax -= 24f; // Account for the scrollbar on the right side
                scrollRect.xMin -= 12f;

                float rowHeight = 86f;
                float geneWidth = 72f;
                RectRow row = new RectRow(scrollRect.xMin, 0, rowHeight, UIDirection.RightThenDown, scrollRect.width, 4f);
                row.RowGapExtra = 36f;

                List<KeyValuePair<string, int>> genesList = genesEligibleForMutation.ToList();
                foreach (string gene in new List<string>(GeneUtils.AllGenesCache.Keys))
                {
                    // If the current gene isn't loaded (such as if it's from another mod that isn't currently running), skip it.
                    if (!GeneUtils.IsCached(gene))
                    {
                        Log.Warning($"Could not find def for gene {gene}. Is it from a mod that is no longer loaded?");
                        continue;
                    }

                    if (gene == CloneDefs.Clone.defName)
                    {
                        // Skip the Clone gene added by this mod. It wouldn't make any sense for a clone to randomly gain the Clone gene
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
                    Widgets.DrawTexturePart(checkboxRect, new Rect(0,0,1,1), Widgets.GetCheckboxTexture(eligible));

                    // Draw the weight slider and its label
                    if (eligible)
                    {
                        // Draw the weight slider
                        Rect sliderRect = new Rect(buttonRect.x, buttonRect.y + rowHeight + 24f, buttonRect.width, 24f);
                        genesEligibleForMutation[gene] = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, genesEligibleForMutation[gene], 0, 10));

                        // Draw the label for the slider
                        Rect sliderLabelRect = new Rect(buttonRect.x+4f, buttonRect.y + rowHeight, buttonRect.width, 24f);
                        Widgets.Label(sliderLabelRect, "Cloning_Settings_MutationGeneWeight".Translate() + ": " + genesEligibleForMutation[gene]);
                    }

                    // Lastly, handle clicking on this gene, by flipflopping its eligibility status
                    if (Widgets.ButtonInvisible(buttonRect))
                    {
                        GeneUtils.SetEligible(gene, !eligible);
                        eligible = !eligible;
                    }
                }

                UIUtility.EndScrollView(scrollList, ref scrollHeight);

                UIUtility.DoOutdent(listingStandard);
            }
            #endregion Mutation Settings

            listingStandard.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref cloningCooldown, "cloningCooldown", true);
            Scribe_Values.Look(ref cloningCooldownDays, "cloningCooldownDays", defaultCooldownDays);
            Scribe_Values.Look(ref doRandomMutations, "doRandomMutations", true);
            Scribe_Values.Look(ref randomMutationChance, "randomMutationChance", 0.3f);
            Scribe_Values.Look(ref maxMutations, "maxMutations", 3);
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
        public static void DoIndent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth -= amount;
            listing.Indent(amount);
        }
        public static void DoOutdent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth += amount;
            listing.Outdent(amount);
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

