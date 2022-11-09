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
using UnityEngine;

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
    class Patch_PregnancyUtility_ApplyBirthOutcome
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
                    yield return CodeInstruction.Call(typeof(Patch_PregnancyUtility_ApplyBirthOutcome), "GeneratePawn");
                }

                // And resume emitting the original code (Including the original call to GeneratePawn, using my now-modified request that's on the stack)
                // Doing it this way should ensure mod compatibility with other mods that touch pawn generation requests
                yield return codes[i];
            }
        }
        /* Original (unpatched Vanilla 1.4) method, truncated to end where the transpiler begins because it's 952 lines in dnSpy:
        public static Thing ApplyBirthOutcome(OutcomeChance outcome, float quality, Precept_Ritual ritual, List<GeneDef> genes, Pawn geneticMother, Thing birtherThing, Pawn father = null, Pawn doctor = null, LordJob_Ritual lordJobRitual = null, RitualRoleAssignments assignments = null)
		{
			Pawn birtherPawn = birtherThing as Pawn;
			Building_GrowthVat building_GrowthVat = birtherThing as Building_GrowthVat;
			if (birtherThing.Spawned)
			{
				EffecterDefOf.Birth.Spawn(birtherThing, birtherThing.Map, 1f);
			}
			bool babiesAreHealthy = Find.Storyteller.difficulty.babiesAreHealthy;
			int positivityIndex = outcome.positivityIndex;
			bool flag = birtherPawn != null && Rand.Chance(PregnancyUtility.ChanceMomDiesDuringBirth(quality)) && (birtherPawn.genes == null || !birtherPawn.genes.HasGene(GeneDefOf.Deathless));
			PawnKindDef kind = ((geneticMother != null) ? geneticMother.kindDef : null) ?? PawnKindDefOf.Colonist;
			Faction faction = birtherThing.Faction;
			PawnGenerationContext context = PawnGenerationContext.NonPlayer;
			int tile = -1;
			bool forceGenerateNewPawn = false;
			bool allowDead = false;
			bool allowDowned = true;
			bool canGeneratePawnRelations = true;
			bool mustBeCapableOfViolence = false;
			float colonistRelationChanceFactor = 1f;
			bool forceAddFreeWarmLayerIfNeeded = false;
			bool allowGay = true;
			bool allowPregnant = false;
			bool allowFood = true;
			bool allowAddictions = true;
			bool inhabitant = false;
			bool certainlyBeenInCryptosleep = false;
			bool forceRedressWorldPawnIfFormerColonist = false;
			bool worldPawnFactionDoesntMatter = false;
			float biocodeWeaponChance = 0f;
			float biocodeApparelChance = 0f;
			Pawn extraPawnForExtraRelationChance = null;
			float relationWithExtraPawnChanceFactor = 1f;
			Predicate<Pawn> validatorPreGear = null;
			Predicate<Pawn> validatorPostGear = null;
			IEnumerable<TraitDef> forcedTraits = null;
			IEnumerable<TraitDef> prohibitedTraits = null;
			float? minChanceToRedressWorldPawn = null;
			float? fixedBiologicalAge = null;
			float? fixedChronologicalAge = null;
			Gender? fixedGender = null;
			string fixedLastName = PregnancyUtility.RandomLastName(geneticMother, birtherThing as Pawn, father);
			string fixedBirthName = null;
			RoyalTitleDef fixedTitle = null;
			Ideo fixedIdeo = null;
			bool forceNoIdeo = true;
			bool forceNoBackstory = false;
			bool forbidAnyTitle = false;
			bool forceDead = positivityIndex == -1;
			List<GeneDef> forcedXenogenes = null;
			XenotypeDef baseliner = XenotypeDefOf.Baseliner;
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, context, tile, forceGenerateNewPawn, allowDead, allowDowned, canGeneratePawnRelations, mustBeCapableOfViolence, colonistRelationChanceFactor, forceAddFreeWarmLayerIfNeeded, allowGay, allowPregnant, allowFood, allowAddictions, inhabitant, certainlyBeenInCryptosleep, forceRedressWorldPawnIfFormerColonist, worldPawnFactionDoesntMatter, biocodeWeaponChance, biocodeApparelChance, extraPawnForExtraRelationChance, relationWithExtraPawnChanceFactor, validatorPreGear, validatorPostGear, forcedTraits, prohibitedTraits, minChanceToRedressWorldPawn, fixedBiologicalAge, fixedChronologicalAge, fixedGender, fixedLastName, fixedBirthName, fixedTitle, fixedIdeo, forceNoIdeo, forceNoBackstory, forbidAnyTitle, forceDead, forcedXenogenes, (genes != null) ? genes : PregnancyUtility.GetInheritedGenes(father, geneticMother), baseliner, null, null, 0f, DevelopmentalStage.Newborn, null, null, null, false));
        
         * ... Truncated
         */

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
            if (!CloneUtils.HasCloneGene(genes)) return request;

            // First copy basic data from the donor 
            Pawn donor = geneticMother ?? father; // Including the father might be overkill, but let's just be safe
            if (donor == null)
            {
                Log.Error("Tried to modify the clone's PawnGenerationRequest, but was unable to determine the donor pawn (both parents were null).");
                return request;
            }

            request.FixedGender = donor.gender;
            request.CanGeneratePawnRelations = false;

            // Now, add the previously-chosen xenotype to the new pawn
            // Try to get a reference to the embryo
            CloneData cloneData = null;
            if (birtherThing is Building_GrowthVat growthVat)
            {
                HumanEmbryo embryo = growthVat.selectedEmbryo;
                cloneData = embryo.TryGetComp<Comp_CloneEmbryo>().cloneData;
            }
            else if (birtherThing is Pawn birtherPawn)
            {
                cloneData =  CloneUtils.GetCloneHediffCompFromPregnancy(birtherPawn).cloneData;
            }
            request.ForcedXenogenes = cloneData?.forcedXenogenes.GenesListForReading;

            /* Disabled in favor of assigning a custom xenotype for every clone
            if (donor.genes.UniqueXenotype)
            {
                request.ForcedCustomXenotype = CloneUtils.CopyCustomXenotypeFrom(donor);
            }
            else
            {
                if (donor.genes.Xenotype == null) Log.Error("Tried to copy non-custom xenotype from donor parent, but it was null.");
                else request.ForcedXenotype = donor.genes.Xenotype;
            }*/

            GeneUtils.TryAddMutationsToRequest(ref request);

            /* Disabled, xenogenes are assigned by the user when the clone is created
            if (CloningSettings.cloneXenogenes && donor.genes.Xenogenes.Count > 0)
            {
                if (request.ForcedXenogenes == null)
                {
                    request.ForcedXenogenes = new List<GeneDef>();
                }
                foreach (Gene gene in donor.genes.Xenogenes)
                {
                    if (gene.def.displayCategory == GeneCategoryDefOf.Archite && !CloningSettings.cloneArchiteGenes) continue; // Skip archite genes unless setting to copy them is enabled

                    if (!request.ForcedXenogenes.Contains(gene.def))
                    {
                        request.ForcedXenogenes.Add(gene.def);
                    }
                }
            }*/
            

            return request;
        }

        /// <summary>
        /// Postfix patch that runs after the actual Pawn instance has been generated, responsible for modifying the newborn pawn
        /// to ensure it matches the parent.
        /// </summary>
        static void Postfix(ref Thing __result, Pawn geneticMother, Pawn father)
        {
            if (!( __result is Pawn pawn )) return; // If the clone was stillborn.
            if (CloneUtils.HasCloneGene(pawn)) return; // Not a clone, ignore

            // Copy basic things from the parent.
            Pawn donor = geneticMother != null ? geneticMother : father;
            if (donor != null) // Shouldn't ever happen but let's just be safe
            {
                pawn.story.headType = donor.story.headType;
                pawn.story.skinColorOverride = donor.story.skinColorOverride;
                pawn.story.furDef = donor.story.furDef;
                if (CloningSettings.inheritHair)
                {
                    pawn.story.hairDef = donor.story.hairDef;
                    pawn.style.beardDef = donor.style.beardDef;
                }
                pawn.style.Notify_StyleItemChanged();
            }

            //FIXME: Disabled temporarily because it causes babies to die
            //if (Settings.cloningCooldown) GeneUtility.ExtractXenogerm(pawn, Mathf.RoundToInt(60000f * Settings.CloneExtractorRegrowingDurationDaysRange.RandomInRange));
        }
    }
}
