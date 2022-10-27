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
        public static Dictionary<string, int> defaultGenesEligible = new Dictionary<string, int>
        {
            {"Inbred", 1},
            {"Instability_Mild", 1},
            {"Instability_Major", 1},
            {"Sterile", 10},
            {"Aggression_Aggressive", 1},
            {"Aggression_HyperAggressive", 1}
        };

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

        public static GeneDef GeneNamed(string gene)
        {
            return AllGenesCache[gene];
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
            else
            {
                Settings.genesEligibleForMutation.Remove(gene);
            }
        }

        static void CacheGeneDefs()
        {
            List<GeneDef> allGenes = DefDatabase<GeneDef>.AllDefsListForReading;
            Log.Message($"Preparing gene cache for Cloning. Found {allGenes.Count} GeneDefs loaded.");

            foreach (GeneDef gene in allGenes)
            {
                allGenesCache.Add(gene.defName, gene);
            }

            genesCached = true;
        }

        //TODO: remove this prob
        static GeneUtils()
        {
        }

        public static void ApplyRandomMutations(ref GeneSet genes)
        {

        }

        public static GeneDef PickRandomMutation()
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

                defName = Settings.genesEligibleForMutation.RandomElementByWeight(x => x.Value).Key;

                Log.Message($"Chose gene {defName}. Searching for the def in the cache");
                if (AllGenesCache.ContainsKey(defName))
                {
                    Log.Message("Found gene in the cache.");
                    return AllGenesCache[defName];
                }

                tries++;
            }

            return null;
        }

        public static List<GeneDef> GetRandomMutations()
        {
            List<GeneDef> genes = new List<GeneDef>();

            for (int i = 0; i < Settings.maxMutations; i++)
            {
                if (Verse.Rand.Chance(Settings.randomMutationChance))
                {
                    GeneDef mutation = PickRandomMutation();
                    if (mutation == null)
                    {
                        Log.Error("Mutation GeneDef was null");
                        continue;
                    }
                    genes.Add(mutation);
                }
            }

            return genes;
        }
    }
}
