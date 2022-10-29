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
    /// Patch to modify the PawnGenerationRequest created for newborn pawns.<br />
    /// Transpiler intercepts the call to GeneratePawn() and modifies the request to match clone genetic rules,
    /// before passing it back to the vanilla function to handle the rest. <br /><br />
    /// Postfix runs after the actual Pawn has been generated based on that PawnGenerationRequest and modifies the Pawn instance to ensure
    /// that it copies everything from the parent correctly.
    /// </summary>
    [HarmonyPatch(typeof(PregnancyUtility))]
    [HarmonyPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    class ApplyBirthOutcome_Patch
    {
        /// <summary>
        /// The method used by <see cref="Transpiler(IEnumerable{CodeInstruction})"/> to intercept the PawnGenerationRequest and modify it. <br />
        /// Right hand side gets the MethodInfo of the PawnGenerator method GeneratePawn overload that takes a PawnGenerationRequest argument. <br />
        /// When this method is called, the most recently-added thing to the stack will be the PawnGenerationRequest argument, which we want to modify.
        /// </summary>
        static MethodInfo anchorMethod = typeof(PawnGenerator).GetMethod("GeneratePawn", new Type[] { typeof(PawnGenerationRequest) });

        /// <summary>
        /// Transpiler that intercepts the PawnGenerationRequest right after it is created and before it is used by GeneratePawn. <br />
        /// Loads a few extra arguments onto the stack, and then calls
        /// <see cref="GeneratePawn(PawnGenerationRequest, Thing, List{GeneDef}, Pawn, Pawn)"/>, which takes those extra arguments
        /// and uses them to modify the request, returning it (Which pushes it back onto the stack, modified). The vanilla GeneratePawn
        /// is then called using the newly-modified version on the stack and generates the pawn.
        /// </summary>
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
            if (CloneUtils.HasCloneGene(genes))
            {
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

                GeneUtils.TryAddMutationsToRequest(ref request);
            }

            return request;
        }

        /// <summary>
        /// Postfix patch that runs after the actual Pawn instance has been generated, responsible for modifying the newborn pawn
        /// to ensure it matches the parent.
        /// </summary>
        static void Postfix(ref Thing __result, Pawn geneticMother, Pawn father)
        {
            /* After the vanilla method has run, go over things to make SURE that the newly-generated pawn copied over all of its genes from the donor
            Here are some probably relevant lines from the vanilla function:

            XenotypeDef xenotype;
            if (PregnancyUtility.TryGetInheritedXenotype(geneticMother, father, out xenotype))
                pawn1.genes?.SetXenotypeDirect(xenotype);
            else if (PregnancyUtility.ShouldByHybrid(geneticMother, father))
            {
                pawn1.genes.hybrid = true;
                pawn1.genes.xenotypeName = (string) "Hybrid".Translate();
            }
            ...
            if (pawn1.RaceProps.IsFlesh)
            {
            if (geneticMother != null)
                pawn1.relations.AddDirectRelation(PawnRelationDefOf.Parent, geneticMother);
            if (father != null)
                pawn1.relations.AddDirectRelation(PawnRelationDefOf.Parent, father);
            if (birtherPawn != null && birtherPawn != geneticMother)
                pawn1.relations.AddDirectRelation(PawnRelationDefOf.ParentBirth, birtherPawn);
            }
            */
            //TODO: Ensure that newborn clones copy body type and hair color genes from donor
            //TODO: Ensure that newborn clones inherit the same body type and hair style from donor, regardless of genes. Probably needs a different patch for when they reach adulthood.

        }
    }
}
