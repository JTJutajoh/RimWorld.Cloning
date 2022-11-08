using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Dark.Cloning
{
    /// <summary>
    /// Tracker for helping pass clone info along from donor to embryo to resultant pawn<br />
    /// Currently helps track:<br />
    /// Forced xenogenes
    /// </summary>
    public class CloneTrackerWorldComponent : WorldComponent
    {
        #region Singleton pattern
        public static CloneTrackerWorldComponent instance;
        public static CloneTrackerWorldComponent Instance => instance;

        public CloneTrackerWorldComponent(World world) : base(world)
        {
            CloneTrackerWorldComponent.instance = this;
            this.Init();
        }
        #endregion Singleton pattern

        private Dictionary<int, CloneData> trackedClones = new Dictionary<int, CloneData>();

        private void Init()
        {

        }

        public static void Track(int hash, CloneData data)
        {
            if (instance.trackedClones.ContainsKey(hash))
                Log.Warning($"{instance.GetType()} tracking a hash code that was already tracked. The previous data will be overwritten. Hash: {hash}");

            instance.trackedClones[hash] = data;
        }

        public static void UnTrack(int hash)
        {
            if (!instance.trackedClones.ContainsKey(hash)) return;

            instance.trackedClones.Remove(hash);
        }

        public static CloneData DataFor(int hash)
        {
            if (!instance.trackedClones.ContainsKey(hash)) return null;

            return instance.trackedClones[hash];
        }
        public static CloneData DataFor(HumanEmbryo embryo)
        {
            return DataFor(embryo.GetHashCode());
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref trackedClones, "trackedClones");
        }
    }

    
}
