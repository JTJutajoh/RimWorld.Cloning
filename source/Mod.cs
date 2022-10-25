using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;

namespace RimWorld
{
    class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            Harmony harmony = new Harmony("Dark.Cloning");

            harmony.PatchAll();
            Log.Message("Cloning loaded");
        }
    }
}
