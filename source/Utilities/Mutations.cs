using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Dark.Cloning
{
    static class Mutations
    {
        public static readonly Dictionary<string, int> defaultGenesEligible = new Dictionary<string, int>
        {
            {"Inbred", 4},
            {"Instability_Mild", 8},
            {"Instability_Major", 4},
            {"Sterile", 10},
            {"Aggression_Aggressive", 4},
            {"Aggression_HyperAggressive", 3},
            {"Learning_Slow", 3},
            {"Nearsighted", 5},
            {"WoundHealing_Slow", 1},
            {"Immunity_Weak", 2},
            {"PsychicAbility_Deaf", 1},
            {"PsychicAbility_Dull", 2},
            {"Mood_Depressive", 1},
            {"Mood_Pessimist", 2},
            {"Delicate", 3},
            {"Pain_Extra", 2}
        };

        public static bool IsEligible(string gene)
        {
            return CloningSettings.genesEligibleForMutation.ContainsKey(gene);
        }

        public static void SetEligible(string gene, bool newEligible)
        {
            // First return early if the desired state is already the current state
            bool isEligible = IsEligible(gene);
            if (( newEligible && isEligible ) || ( !newEligible && !isEligible )) return;
            if (newEligible)
            {
                CloningSettings.genesEligibleForMutation.Add(gene, 1);
            }
            else if (isEligible)
            {
                CloningSettings.genesEligibleForMutation.Remove(gene);
            }
        }

        private static GeneDef PickRandomMutation(Dictionary<string,int> options)
        {
            string defName = "";

            int tries = 0;

            while (true)
            {
                if (tries > 20)
                {
                    Log.Error("Error picking gene for random mutation. Tried 20 times.");
                    break;
                }

                defName = options.RandomElementByWeight(x => x.Value).Key;

                if (GeneUtils.AllGenesCache.ContainsKey(defName))
                {
                    return GeneUtils.AllGenesCache[defName];
                }

                tries++;
            }

            return null;
        }

        public static List<GeneDef> GetRandomMutations()
        {
            List<GeneDef> genes = new List<GeneDef>();
            // First do a dice roll to see if we get any mutations at all
            if (!Verse.Rand.Chance(CloningSettings.randomMutationChance)) return genes;

            Dictionary<string, int> geneOptions = CloningSettings.genesEligibleForMutation;
            int numMutations = CloningSettings.numMutations.RandomInRange;

            for (int i = 0; i < numMutations; i++)
            {
                if (geneOptions.Count <= 0) break; // There are no options, either because the user didn't set any or because we exhausted them
                GeneDef mutation = PickRandomMutation(geneOptions);
                if (mutation == null)
                {
                    Log.Error("Mutation GeneDef was null");
                    continue;
                }
                genes.Add(mutation);
                geneOptions.Remove(mutation.defName);
                // Iterate over the list looking for options that would interfere with the chosen gene, and remove them from the options
                // First make a copy so we can modify it
                Dictionary<string, int> tmpDict = new Dictionary<string, int>();
                foreach (KeyValuePair<string, int> item in geneOptions)
                {
                    tmpDict.Add(item.Key, item.Value);
                }

                foreach (string option in geneOptions.Keys)
                {
                    GeneDef optionDef = GeneUtils.AllGenesCache[option];
                    // If these genes override each other in either direction
                    if (optionDef.Overrides(mutation,false, false) || mutation.Overrides(optionDef, false, false))
                    {
                        tmpDict.Remove(option);
                    }
                }
                geneOptions = tmpDict;
            }

            return genes;
        }
        /// <summary>
        /// Conditionally adds a random set of mutation genes to the given request, according to Settings.
        /// </summary>
        /// <returns>Modified version of the given PawnGenerationRequest with mutation genes added</returns>
        public static void TryAddMutationsToRequest(ref PawnGenerationRequest request)
        {
            if (!CloningSettings.doRandomMutations || CloningSettings.genesEligibleForMutation.Count <= 0)
                return;

            bool addAsXeno = CloningSettings.addMutationsAsXenogenes;
            // Get the existing list of forced genes, if there are any, intializing a new empty list if not
            List<GeneDef> forcedGenes = addAsXeno ? request.ForcedXenogenes : request.ForcedEndogenes;
            if (forcedGenes == null)
                forcedGenes = new List<GeneDef>();

            List<GeneDef> mutations = GetRandomMutations();
            for (int i = 0; i < mutations.Count; i++)
            {
                if (!forcedGenes.Contains(mutations[i])) 
                    forcedGenes.Add(mutations[i]);
            }

            // Send a letter to the player that mutation(s) occurred 
            if (mutations.Count > 0)
            {
                StringBuilder stringBuilder = new StringBuilder("Cloning_Letter_MutationText".Translate());
                for (int i = 0; i < mutations.Count; i++)
                    stringBuilder.AppendInNewLine(" - " + mutations[i].LabelCap);
                Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter("Cloning_Letter_MutationLabel".Translate(), stringBuilder.ToString(), LetterDefOf.NeutralEvent));
            }

            // Apply the mutated list to the request
            if (addAsXeno)
                request.ForcedXenogenes = forcedGenes;
            else
                request.ForcedEndogenes = forcedGenes;
        }
    }
}
