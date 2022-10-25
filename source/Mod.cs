using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace Dark.Cloning
{
    class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            GetSettings<Settings>().DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Cloning_SettingsCategory".Translate();
        }
    }

    [StaticConstructorOnStartup]
    static class LoadHarmony
    {
        static LoadHarmony()
        {
            Harmony harmony = new Harmony("Dark.Cloning");

            harmony.PatchAll();
        }
    }
}
