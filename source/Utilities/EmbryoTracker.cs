using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Dark.Cloning
{
    /// <summary>
    /// Simple class that contains a static list of embryos that were created as clones, as their HashCodes. <br />
    /// Used to detect within patched methods if the embryo should gain the Clone gene, since the geneSet is protected and initialized as null. <br />
    /// This should only be relied on if you're sure that the embryo was just created on this same tick, since embryos can be destroyed (such as when
    /// implanted in a mother) so the hash may not match later. 
    /// </summary>
    public static class EmbryoTracker
    {
        private static List<int> embryoHashes = new List<int>();

        /// <summary>
        /// Add an embryo to the list of tracked embryos, as its hash.
        /// </summary>
        /// <param name="embryo">Reference to the embryo to be tracked</param>
        /// <param name="forcedXenogenes">Optional GeneSet that contains a list of xenogenes to force</param>
        public static void Track(HumanEmbryo embryo)
        {
            int hash = embryo.GetHashCode();
            embryoHashes.Add(hash);

            //Log.Message($"EmbryoTracker tracking embryo {embryo.ToString()} with hash {hash}. {embryoHashes.Count} embryos tracked.");
        }

        /// <summary>
        /// Checks if the supplied embryo has been tracked previously. 
        /// <br />If so, also removes it from the list.
        /// </summary>
        /// <param name="embryo">Reference to the embryo to look for</param>
        public static bool Contains(HumanEmbryo embryo)
        {
            int hash = embryo.GetHashCode();
            bool result = embryoHashes.Contains(hash);

            if (result) embryoHashes.Remove(hash);

            //Log.Message($"EmbryoTracker checking for embryo {embryo.ToString()} with hash {hash}: {result}. {embryoHashes.Count} embryos tracked.");

            return result;
        }
    }
}
