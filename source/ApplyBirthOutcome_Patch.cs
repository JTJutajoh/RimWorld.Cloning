using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

/*
 * psudocode From Bradson:

foreach (code in codes)
{
    if (code.Calls(PawnGeneratorMethod))
    {
        yield return extraArgument;
        yield return methodThatReturnsModifiedRequest(request, extraArgument);
    }

    yield return code;
}

*/

namespace Dark.Cloning
{
    [HarmonyPatch(typeof(PregnancyUtility))]
    [HarmonyPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    class ApplyBirthOutcome_Patch
    {
        //static CodeInstruction anchorMethod = CodeInstruction.Call(typeof(PawnGenerator), "GeneratePawn");
        static Type[] anchorMethodArgTypes = new Type[1] { typeof(PawnGenerationRequest) };
        static CodeInstruction anchorMethod = CodeInstruction.Call(typeof(PawnGenerator), "GeneratePawn", anchorMethodArgTypes);

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Log.Message("Running patch on ApplyBirthOutcome...");
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                //TODO: Check if it matches the method call op code for the method I want
                Log.Message($"Checking code {codes[i].ToString()}");
                if (codes[i] == anchorMethod)
                {
                    Log.Message("Found the method call.");
                }
            }

            return codes.AsEnumerable();
        }
    }
}
