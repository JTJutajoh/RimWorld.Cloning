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

namespace Dark.Cloning
{
    /// <summary>
    /// Patch to modify the PawnGenerationRequest created for newborn pawns. 
    /// Transpiler intercepts the call to GeneratePawn() and modifies the request to match clone genetic rules
    /// </summary>
    [HarmonyPatch(typeof(PregnancyUtility))]
    [HarmonyPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    class ApplyBirthOutcome_Patch
    {
        static MethodInfo anchorMethod = typeof(PawnGenerator).GetMethod("GeneratePawn", new Type[] { typeof(PawnGenerationRequest) });

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Log.Message("Cloning Patching...");

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {                
                if (codes[i].Calls(anchorMethod))
                {
                    // First, load the additional argument(s) onto the stack
                    yield return new CodeInstruction(OpCodes.Ldarg, 5); // Thing birtherThing
                    yield return new CodeInstruction(OpCodes.Ldarg, 3); // List<GeneDef> genes
                    yield return new CodeInstruction(OpCodes.Ldarg, 4); // Pawn geneticMother
                    yield return new CodeInstruction(OpCodes.Ldarg, 6); // Pawn father

                    // Then call my custom method instead of the original one.
                    yield return CodeInstruction.Call(typeof(ApplyBirthOutcome_Patch), "GeneratePawn");
                }

                // And resume emitting the original code (Including the original call to GeneratePawn, using my now-modified request that's on the stack)
                // Doing it this way should ensure mod compatibility with other mods that touch pawn generation requests
                yield return codes[i];
            }
        }

        /// <summary>
        /// Custom method that intercepts the PawnGenerationRequest and modifies it if the pawn to be generated is a clone.
        /// </summary>
        /// <param name="request">The original request to be modified</param>
        /// <param name="birtherThing">The Thing that gave birth to this pawn. Either the growth vat or the mother.</param>
        /// <param name="genes">List of all genes the embryo was previously given, either by vanilla code for this mod's custom patch</param>
        /// <param name="geneticMother">The mother</param>
        /// <param name="father">The father, which might always be null. But it is included here just in case.</param>
        /// <returns>The modified PawnGenerationRequest, ready to be pushed back onto the stack and sent to GeneratePawn</returns>
        static PawnGenerationRequest GeneratePawn(PawnGenerationRequest request, Thing birtherThing, List<GeneDef> genes, Pawn geneticMother, Pawn father)
        {
            Log.Message("GeneratePawn intercepted on pawn birth. Running patch to check if clone");

            if (CloneUtils.HasCloneGene(genes))
            {
                Log.Message("Newborn is a clone, modifying its genetics.");
                Pawn donor = geneticMother ?? father; // Including the father might be overkill, but let's just be safe
                if (donor == null)
                {
                    Log.Error("Tried to modify the clone's PawnGenerationRequest, but was unable to determine the donor pawn (both parents were null).");
                    return request;
                }

                // Modify the request based on the found genes. 
                request.FixedGender = donor.gender;

                if (donor.genes.UniqueXenotype)
                {
                    request.ForcedCustomXenotype = CloneUtils.CopyCustomXenotypeFrom(donor);
                }
                else
                {
                    if (donor.genes.Xenotype == null) Log.Error("Tried to copy non-custom xenotype from donor parent, but it was null.");
                    else request.ForcedXenotype = donor.genes.Xenotype;
                }

                if (Settings.doRandomMutations)
                {
                    request.ForcedXenogenes = CloneMutations.GetRandomMutations();
                }
            }

            return request;
        }
    }
}
