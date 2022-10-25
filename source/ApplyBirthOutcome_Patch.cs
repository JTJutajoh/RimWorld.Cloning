using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
        static MethodInfo anchorMethod = typeof(PawnGenerator).GetMethod("GeneratePawn", new Type[] { typeof(PawnGenerationRequest) });

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Log.Message("Running patch on ApplyBirthOutcome...");

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {                
                if (codes[i].Calls(anchorMethod))
                {
                    // First, load the additional argument(s) onto the stack
                    yield return new CodeInstruction(OpCodes.Ldarg, 5); // Thing birtherThing
                    yield return new CodeInstruction(OpCodes.Ldarg, 4); // Pawn geneticMother

                    yield return CodeInstruction.Call(typeof(ApplyBirthOutcome_Patch), "GeneratePawn");
                }

                yield return codes[i];
            }
        }

        static PawnGenerationRequest GeneratePawn(PawnGenerationRequest request, Thing birtherThing, Pawn geneticMother)
        {
            Log.Message("Custom GeneratePawn running");
            Building_GrowthVat building_GrowthVat = birtherThing as Building_GrowthVat;
            Pawn mother = birtherThing as Pawn;
            
            Comp_Clone cloneComp;

            if (building_GrowthVat != null)
            {
                Log.Message("birtherThing was a growth vat.");
                Log.Message("Embryo: " + building_GrowthVat.selectedEmbryo.ToString());
                cloneComp = building_GrowthVat.selectedEmbryo.TryGetComp<Comp_Clone>();
                Log.Message($"Is this embryo a clone? {cloneComp.IsClone}");
            }

            else if (mother != null)
            {
                Log.Message("birtherThing was a human.");
                Hediff pregnancy = PregnancyUtility.GetPregnancyHediff(mother);
                ((Hediff_Pregnant) pregnancy).
                Log.Message("Embryo: " + mother.selectedEmbryo.ToString());
            }

            return request;
        }
    }
}
