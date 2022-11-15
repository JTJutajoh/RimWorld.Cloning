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

        /// <summary>
        /// Helper method to get all of the genes in a list of genepacks. Does not check for duplicates
        /// </summary>
        public static List<GeneDef> GetAllGenesInPacks(List<Genepack> genepacks)
        {
            List<GeneDef> geneSet = new List<GeneDef>();
            for (int i = 0; i < genepacks.Count; i++)
            {
                if (genepacks[i].GeneSet != null)
                {
                    List<GeneDef> genes = genepacks[i].GeneSet.GenesListForReading;
                    for (int j = 0; j < genes.Count; j++)
                    {
                        geneSet.Add(genes[j]);
                    }
                }
            }
            return geneSet;
        }

        public static Genepack GetXenogenesAsGenepack(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("Tried to get xenogenes from null pawn");
                return null;
            }

            
            Genepack genepack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);
            List<GeneDef> genesToAdd = new List<GeneDef>();

            if (pawn.genes != null && pawn.genes.Xenogenes != null)
            {
                if (pawn.genes.Xenogenes.Count == 0)
                    return null;
                foreach (Gene gene in pawn.genes.Xenogenes)
                {
                    genesToAdd.Add(gene.def);
                }
            }

            genepack.Initialize(genesToAdd);


            return genepack;
        }

        public static void AddGeneSet(ref GeneSet a, GeneSet b)
        {
            for (int i = 0; i < b.GenesListForReading.Count; i++)
            {
                a.AddGene(b.GenesListForReading[i]);
            }
        }

        public static List<GeneDef> GetGenesAsDefs(List<Gene> genes)
        {
            var result = new List<GeneDef>();
            foreach (Gene gene in genes)
            {
                result.Add(gene.def);
            }
            return result;
        }

        public static List<GeneDef> GetGeneDefsNamed(List<string> genes)
        {
            var result = new List<GeneDef>();
            foreach (string gene in genes)
            {
                result.Add(GeneNamed(gene));
            }
            return result;
        }
    }
}
