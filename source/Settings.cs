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

        public static Dictionary<string, int> genesEligibleForMutation = CloneMutations.defaultGenesEligible;

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
            Scribe_Values.Look(ref genesEligibleForMutation, "genesEligibleForMutation", CloneMutations.defaultGenesEligible);


            base.ExposeData();
        }
    }
}

