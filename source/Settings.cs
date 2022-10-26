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

            listingStandard.CheckboxLabeled("Cloning_Settings_CloningCooldown".Translate(), ref Settings.cloningCooldown, "Cloning_Cooldown_Description".Translate());
            listingStandard.Label("Cloning_Cooldown_Description".Translate());
            DoIndent(listingStandard);
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
            DoOutdent(listingStandard);

            listingStandard.GapLine();

            listingStandard.CheckboxLabeled("Cloning_Settings_DoMutations".Translate(), ref Settings.doRandomMutations);
            listingStandard.Label("Cloning_Mutations_Description".Translate());
            DoIndent(listingStandard);
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
            }


            // Begin scrollview for gene mutation settings
            Rect scrollRect;

            scrollRect = inRect.BottomPart(0.6f);
            scrollRect = scrollRect.RightPart(0.6f);
            scrollRect.yMax -= 50f;

            // Draw the button to add new genes
            float listHeaderSize = 20f;
            Rect listHeaderRect = new Rect();
            listHeaderRect.xMax = scrollRect.xMax;
            listHeaderRect.yMin = scrollRect.yMin - listHeaderSize;
            listHeaderRect.yMax = listHeaderRect.yMin + listHeaderSize;
            listHeaderRect.xMin -= scrollRect.xMin + 32f;
            Widgets.Label(listHeaderRect, "Cloning_Settings_GeneListHeader".Translate());

            // Draw the background for the sroll box
            Widgets.DrawBoxSolid(scrollRect, new Color(0.7f, 0.7f, 0.7f, 0.3f));
            scrollRect = scrollRect.ContractedBy(8f);


            UIUtility.MakeAndBeginScrollView(scrollRect, scrollHeight, ref scrollPos, out Listing_Standard scrollList);

            Color rowColorA = new Color(0.2f, 0.2f, 0.2f);
            Color rowColorB = new Color(0.35f, 0.35f, 0.35f);
            bool useColorA = true;

            List<KeyValuePair<string, int>> genesList = genesEligibleForMutation.ToList();
            //foreach (KeyValuePair<string, int> gene in genesList)
            foreach (string gene in new List<string>(GeneUtils.AllGenesCache.Keys))
            {
                // If the current gene isn't loaded (such as if it's from another mod that isn't currently running), skip it.
                if (!GeneUtils.IsCached(gene))
                {
                    Log.Warning($"Could not find def for gene {gene}. Is it from a mod that is no longer loaded?");
                    continue;
                }

                Rect rowRect = scrollList.GetRect(50f);

                // Draw an alternating background for each row
                Color rowColor;
                if (useColorA) rowColor = rowColorA;
                else rowColor = rowColorB;
                useColorA = !useColorA;
                Widgets.DrawBoxSolid(rowRect, rowColor);

                // Draw the icon of the gene
                Texture2D geneIcon = GeneUtils.IconFor(gene);
                Rect iconRect = new Rect();
                iconRect.xMin = rowRect.xMin;
                iconRect.yMin = rowRect.yMin;
                iconRect.xMax = rowRect.xMin + 50f;
                iconRect.yMax = rowRect.yMax;
                Widgets.DrawTexturePart(iconRect, new Rect(0, 0, 1, 1), geneIcon);

                // Offset the vertical position of rowRect so that the following elements get shifted down
                rowRect.y += 16f;

                // Draw the name of the gene
                Rect labelRect = rowRect.LeftPart(0.5f);
                labelRect.xMin += 64f;
                Widgets.Label(labelRect, GeneUtils.LabelCapFor(gene));

                if (GeneUtils.IsEligible(gene))
                {
                    // Draw the weight slider
                    Rect sliderRect = rowRect.RightPart(0.5f);
                    sliderRect.xMax -= 30f;
                    sliderRect.y += 10f;
                    genesEligibleForMutation[gene] = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, genesEligibleForMutation[gene], 0, 10));
                    //genesEligibleForMutation[gene.Key] = Mathf.RoundToInt(scrollList.Slider(genesEligibleForMutation[gene.Key], 0,10));

                    // Draw the label for the slider
                    string sliderLabel = "Cloning_Settings_MutationGeneWeight".Translate() + ": " + genesEligibleForMutation[gene];

                    Rect sliderLabelRect = new Rect();
                    sliderLabelRect.yMin = rowRect.yMin - 8f;
                    sliderLabelRect.yMax = rowRect.yMax - 8f;
                    sliderLabelRect.xMin = rowRect.xMax - 110f;
                    sliderLabelRect.xMax = rowRect.xMax - 30f;
                    Widgets.Label(sliderLabelRect, sliderLabel);
                }

                // Draw the X button to remove from the list
                bool enabled = GeneUtils.IsEligible(gene);
                Texture2D xTex = Widgets.GetCheckboxTexture(enabled);
                Rect xRect = new Rect();
                float size = 20f;
                xRect.yMin = rowRect.yMin - 2f;
                xRect.xMax = rowRect.xMax - 2f;

                xRect.xMin = xRect.xMax - size;
                xRect.yMax = xRect.yMin + size;
                if (Widgets.ButtonImage(xRect, xTex))
                {
                    if (enabled)
                    {
                        genesEligibleForMutation.Remove(gene);
                    }
                    else
                    {
                        genesEligibleForMutation.Add(gene, 1);
                    }
                }
            }

            UIUtility.EndScrollView(scrollList, ref scrollHeight);

            DoOutdent(listingStandard);


            listingStandard.End();
        }
        private void DoIndent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth -= amount;
            listing.Indent(amount);
        }
        private void DoOutdent(Listing_Standard listing, float amount = 12f)
        {
            listing.ColumnWidth += amount;
            listing.Outdent(amount);
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

    // Copied from https://gitlab.com/nightcorp/nightmarecore/-/blob/master/Source/Utilities/UIUtility.cs#L29
    public static class UIUtility
    {
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

    }
}

