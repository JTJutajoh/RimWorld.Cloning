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
        /// Static reference to the cloneData from an embryo,
        /// so that it can be passed on to the instance of the clone Gene on the newly-generated pawn
        /// </summary>
        static CloneData staticCloneData;

        /// <summary>
        /// Transpiler that intercepts the PawnGenerationRequest right after it is created and before it is used by GeneratePawn. <br />
        /// Loads a few extra arguments onto the stack, and then calls
        /// <see cref="GeneratePawn(PawnGenerationRequest, Thing, List{GeneDef}, Pawn, Pawn)"/>, which takes those extra arguments
        /// and uses them to modify the request, returning it (Which pushes it back onto the stack, modified). The vanilla GeneratePawn
        /// is then called using the newly-modified version on the stack and generates the pawn.
        /// </summary>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
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
                    // The current (original) instruction calls GeneratePawn, which returns the new Pawn (putting it on the stack). 
                    // We want to access the pawn that was just created, so if we call a method that takes a Pawn as an argument it should work
                    // And if that method returns the Pawn again, it goes back on the stack and the method can resume normally
                    //yield return CodeInstruction.Call(typeof(Patch_PregnancyUtility_ApplyBirthOutcome), "ModifyPawn");
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
        /// A method that takes the pawn immediately after it was generated, and returns it again
        /// Disabled, not necessary but maybe in the future.
        /// Commented-out code in the transpiler injects this method immediately after the pawn is generated, before the rest of the method is run
        /// For now, though, the postfix is good enough
        /// </summary>
        static Pawn ModifyPawn(Pawn pawn)
        {
            if (!pawn.IsClone(out CloneGene cloneGene))
            {
                
            }
            return pawn;
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
            // Try to get a reference to the cloneData from the embryo or the pregnancy hediff, which is where the cloneData is stored if there is any
            CloneData cloneData = null;

            // GrowthVats keep the embryo Thing in their innerContainer until birth
            if (birtherThing is Building_GrowthVat growthVat)
            {
                HumanEmbryo embryo = growthVat.selectedEmbryo;
                if (!embryo.IsClone(out Comp_CloneEmbryo cloneComp))
                    return request; // Not a clone
                cloneData = cloneComp.cloneData;
            }
            // Pregnant pawns do not keep the embryo Thing and instead transfer its data to a series of hediffs
            else if (birtherThing is Pawn birtherPawn)
            {
                Hediff hediff = birtherPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLaborPushing);
                if (!hediff.IsClonePregnancy(out HediffComp_Pregnant_Clone cloneComp))
                    return request; // Not a clone pregnancy
                cloneData = cloneComp.cloneData;
            }

            // The above checks should ensure this never happens, but just to be safe
            if (cloneData == null)
            {
                Log.Warning($"Embryo/pregnancy hediff looked like a clone, but had no cloneData");
                return request;
            }

            // Store the data in a static reference so that the postfix can pick it up and apply it to the clone Gene instance on the pawn after it is generated
            Patch_PregnancyUtility_ApplyBirthOutcome.staticCloneData = cloneData;

            request.CanGeneratePawnRelations = false;

            // First copy basic data from the donor 
            request.FixedGender = cloneData.fixedGender;

            // Now, add the previously-chosen xenotype to the new pawn
            //request.ForcedXenogenes = cloneData.forcedXenogenes.GenesListForReading;

            if (cloneData.UniqueXenotype)
            {
                request.ForcedCustomXenotype = cloneData.customXenotype;
            }
            else
            {
                request.ForcedXenotype = cloneData.xenotype;
            }

            Mutations.TryAddMutationsToRequest(ref request);

            return request;
        }

        /// <summary>
        /// Postfix patch that runs after the actual Pawn instance has been generated, responsible for modifying the newborn pawn
        /// to ensure it matches the parent.
        /// </summary>
        static void Postfix(ref Thing __result, Pawn geneticMother, Pawn father)
        {
            if (!( __result is Pawn pawn )) // If the clone was stillborn.
                return;
            if (!pawn.IsClone(out var cloneGene)) // Not a clone, ignore
                return;

            if (staticCloneData == null)
            {
                Log.Error($"Clone gene on pawn {pawn.LabelCap} but the statically-stored cloneData in {nameof(Patch_PregnancyUtility_ApplyBirthOutcome)} has no data.");
                return;
            }

            cloneGene.cloneData = staticCloneData;

            if (cloneGene.cloneData == null)
            {
                Log.Warning($"Tried to set cloneData on Clone gene on pawn {pawn.LabelCap}, but it was null.");
                return;
            }

            // Copy basic things from the parent.
            Pawn donor = cloneGene.cloneData.donorPawn;
            if (donor != null && CloningSettings.CloneRelationship != null && CloningSettings.CloneRelationship != PawnRelationDefOf.Child)
            {
                donor.relations.AddDirectRelation(CloningSettings.CloneRelationship, pawn);
            }
            else if (donor == null)
            {
                Log.Warning($"Tried to add clone relation for new clone {pawn.LabelCap}, but cloneData had null donor reference. Did the donor pawn get cleaned up?");
            }
            

            pawn.story.headType = cloneGene.cloneData.headType;
            pawn.story.skinColorOverride = cloneGene.cloneData.skinColorOverride;
            pawn.story.furDef = cloneGene.cloneData.furDef;
            if (CloningSettings.inheritHair)
            {
                pawn.story.hairDef = cloneGene.cloneData.hairDef;
                pawn.style.beardDef = cloneGene.cloneData.beardDef;
            }
            pawn.style.Notify_StyleItemChanged();

            //FIXME: Disabled temporarily because it causes babies to die
            //if (Settings.cloningCooldown) GeneUtility.ExtractXenogerm(pawn, Mathf.RoundToInt(60000f * Settings.CloneExtractorRegrowingDurationDaysRange.RandomInRange));

            // Clear the static reference for the next time this patch is run
            staticCloneData = null;
        }
    }
}
