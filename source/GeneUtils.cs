using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace Dark.Cloning
{
    [StaticConstructorOnStartup]
    class GeneUtils
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

        #region All Genes Cache
        private static bool genesCached = false;
        // A simple local cache of all the genes loaded in the game
        private static Dictionary<string, GeneDef> allGenesCache = new Dictionary<string, GeneDef>();
        public static Dictionary<string, GeneDef> AllGenesCache 
        {
            get
            {
                if (!genesCached) CacheGeneDefs();
                return allGenesCache;
            }
        }

        private static Dictionary<GeneCategoryDef, List<string>> allGeneCategoriesCache = new Dictionary<GeneCategoryDef, List<string>>();
        public static Dictionary<GeneCategoryDef, List<string>> AllGeneCategoriesCache
        {
            get
            {
                if (!genesCached) CacheGeneDefs();
                return allGeneCategoriesCache;
            }
        }

        static void CacheGeneDefs()
        {
            List<GeneDef> allGenes = DefDatabase<GeneDef>.AllDefsListForReading;
            Log.Message($"Preparing gene cache for Cloning. Found {allGenes.Count} GeneDefs loaded.");

            foreach (GeneDef gene in allGenes)
            {
                allGenesCache.Add(gene.defName, gene);

                // Create the category list if it doesn't exist
                if (!allGeneCategoriesCache.ContainsKey(gene.displayCategory))
                {
                    allGeneCategoriesCache.Add(gene.displayCategory, new List<string>());
                }

                allGeneCategoriesCache[gene.displayCategory].Add(gene.defName);
            }

            genesCached = true;
        }
        #endregion All Genes Cache

        #region Misc Getters
        public static GeneDef GeneNamed(string gene)
        {
            return AllGenesCache[gene];
        }

        public static List<GeneDef> GetAllGenesInCategory(GeneCategoryDef category)
        {
            List<GeneDef> result = new List<GeneDef>();

            return result;
        }

        public static Texture2D IconFor(string gene)
        {
            if (!IsCached(gene)) return BaseContent.BadTex;

            return AllGenesCache[gene].Icon;
        }

        public static string LabelCapFor(string gene)
        {
            if (!IsCached(gene)) return "unknown";

            return AllGenesCache[gene].LabelCap;
        }

        public static GeneCategoryDef CategoryOf(GeneDef gene)
        {

            return gene.displayCategory;
        }
        public static GeneCategoryDef CategoryOf(string gene)
        {
            return CategoryOf(GeneUtils.GeneNamed(gene));
        }

        public static bool IsCached(string gene)
        {
            return AllGenesCache.ContainsKey(gene);
        }

        public static bool IsEligible(string gene)
        {
            return Settings.genesEligibleForMutation.ContainsKey(gene);
        }

        public static void SetEligible(string gene, bool newEligible)
        {
            // First return early if the desired state is already the current state
            bool isEligible = IsEligible(gene);
            if (( newEligible && isEligible ) || ( !newEligible && !isEligible )) return;
            if (newEligible)
            {
                Settings.genesEligibleForMutation.Add(gene, 1);
            }
            else if (isEligible)
            {
                Settings.genesEligibleForMutation.Remove(gene);
            }
        }
        #endregion Misc Getters

        #region Mutation Utils
        public static void ApplyRandomMutations(ref GeneSet genes)
        {

        }
        /// <summary>
        /// Conditionally adds a random set of mutation genes to the given request, according to Settings.
        /// </summary>
        /// <returns>Modified version of the given PawnGenerationRequest with mutation genes added</returns>
        public static PawnGenerationRequest TryAddMutationsToRequest(ref PawnGenerationRequest request)
        {
            List<GeneDef> originalEndoGenes = request.ForcedEndogenes;
            List<GeneDef> originalXenoGenes = request.ForcedXenogenes;
            if (Settings.doRandomMutations && Settings.genesEligibleForMutation.Count > 0)
            {
                List<GeneDef> mutations = GeneUtils.GetRandomMutations();
                if (mutations.Count > 0)
                {
                    string letterContents = "Cloning_Letter_MutationText".Translate();
                    foreach (GeneDef mutation in mutations)
                    {
                        letterContents = letterContents + "\n -" + mutation.LabelCap;
                    }
                    Letter letter = LetterMaker.MakeLetter("Cloning_Letter_MutationLabel".Translate(), letterContents, LetterDefOf.NeutralEvent);
                    Find.LetterStack.ReceiveLetter(letter);
                }
                if (Settings.addMutationsAsXenogenes)
                {
                    foreach (GeneDef gene in mutations)
                    {
                        if (!request.ForcedXenogenes.Contains(gene))
                        {
                            request.ForcedXenogenes.Add(gene);
                        }
                    }
                }
                else
                {
                    foreach (GeneDef gene in mutations)
                    {
                        if (!request.ForcedEndogenes.Contains(gene))
                        {
                            request.ForcedEndogenes.Add(gene);
                        }
                    }
                }
            }
            return request;
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

                if (AllGenesCache.ContainsKey(defName))
                {
                    return AllGenesCache[defName];
                }

                tries++;
            }

            return null;
        }

        public static List<GeneDef> GetRandomMutations()
        {
            List<GeneDef> genes = new List<GeneDef>();
            // First do a dice roll to see if we get any mutations at all
            if (!Verse.Rand.Chance(Settings.randomMutationChance)) return genes;

            Dictionary<string, int> geneOptions = Settings.genesEligibleForMutation;
            int numMutations = Settings.numMutations.RandomInRange;

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
                    GeneDef optionDef = AllGenesCache[option];
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
        #endregion Mutation Utils
    }
}
