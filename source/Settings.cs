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
        public static int cloningCooldownDays = 7;
        private const int defaultCooldownDays = 7;
        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();

            listingStandard.Begin(inRect);

            listingStandard.Label("Cloning_Cooldown_Description".Translate());
            listingStandard.CheckboxLabeled("Cloning_Settings_CloningCooldown".Translate(), ref Settings.cloningCooldown, "Cloning_Cooldown_Description".Translate());
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

            listingStandard.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref cloningCooldown, "cloningCooldown", true);
            Scribe_Values.Look(ref cloningCooldownDays, "cloningCooldownDays", defaultCooldownDays);


            base.ExposeData();
        }
    }
}

