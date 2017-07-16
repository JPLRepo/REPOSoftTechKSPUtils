using System.Collections.Generic;
using RSTUtils;
using UnityEngine;

namespace BackgroundResources
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class UnloadedResources : MonoBehaviour
    {
        public static UnloadedResources Instance;
        public static DictionaryValueList<ProtoVessel, InterestedVessel> InterestedVessels;
        public static List<string> InterestingModules;
        internal bool BackgroundProcessingInstalled = false;
        private bool loggedBackgroundProcessing = false;
        private bool gamePaused = false;

        /// <summary>
        /// Awake method will setup the InterestingModules that this mod will generate ElectricCharge for.
        /// Checks if the BackgroundProcessing Mod is installed and if it is this mod will not generate ElectricCharge.
        /// </summary>
        public void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
            InterestedVessels = new DictionaryValueList<ProtoVessel, InterestedVessel>();
            InterestingModules = new List<string>();
            InterestingModules.Add("ModuleDeployableSolarPanel");
            InterestingModules.Add("ModuleGenerator");
            InterestingModules.Add("KopernicusSolarPanel");
            BackgroundProcessingInstalled = RSTUtils.Utilities.IsModInstalled("BackgroundProcessing");
            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoad);
            GameEvents.onGamePause.Add(onGamePause);
            GameEvents.onGameUnpause.Add(onGameUnPause);
            if (BackgroundProcessingInstalled)
            {
                if (!loggedBackgroundProcessing)
                {
                    Utilities.Log("BackgroundProcessing Mod installed. BackgroundResources not producing unloaded vessel resources.\nIt is recommended you remove BackgroundProcessing mod.");
                    loggedBackgroundProcessing = true;
                }
            }
            if (FlightDriver.Pause)
            {
                onGamePause();
            }
        }

        private void OnDestroy()
        {
            GameEvents.onGamePause.Remove(onGamePause);
            GameEvents.onGameUnpause.Remove(onGameUnPause);
        }

        /// <summary>
        /// If BackgroundProcessing Mod is installed do nothing.
        /// Otherwise as long as time isn't paused we generate ElectricCharge for all cached vessels.
        /// </summary>
        public void FixedUpdate()
        {
            if (BackgroundProcessingInstalled)
            {
                if (!loggedBackgroundProcessing)
                {
                    Utilities.Log("BackgroundProcessing Mod installed. BackgroundResources not producing unloaded vessel resources.\nIt is recommended you remove BackgroundProcessing mod.");
                    loggedBackgroundProcessing = true;
                }
                return;
            }

            if (FlightGlobals.fetch != null && !gamePaused)
            {
                //Generate EC
                Dictionary<ProtoVessel, InterestedVessel>.Enumerator vslenumerator = InterestedVessels.GetDictEnumerator();
                while (vslenumerator.MoveNext())
                {
                    if (vslenumerator.Current.Value.ModuleHandlers.Count > 0)
                    {
                        ProcessInterestedModules(vslenumerator.Current.Value);
                        UpdateResourceCacheOverflows(vslenumerator.Current.Value);
                    }
                }
            }
        }

        private void onGameSceneLoad(GameScenes scene)
        {
            InterestedVessels.Clear();
        }

        private void onGamePause()
        {
            gamePaused = true;
        }

        private void onGameUnPause()
        {
            gamePaused = false;
        }

        /// <summary>
        /// Other mods need to call this to start processing a ProtoVessel.
        /// Will write this into an API in future version.
        /// </summary>
        /// <param name="vessel"></param>
        public void AddInterestedVessel(ProtoVessel vessel)
        {
            if (!InterestedVessels.Contains(vessel))
            {
                InterestedVessel iVessel = new InterestedVessel(vessel.vesselRef, vessel);
                InterestedVessels.Add(vessel, iVessel);
                CacheResources.CreatecachedVesselResources(vessel);
            }
        }

        /// <summary>
        /// Other mods need to call this to stop processing a ProtoVessel.
        /// Will write this into an API in future version.
        /// </summary>
        /// <param name="vessel"></param>
        public void RemoveInterestedVessel(ProtoVessel vessel)
        {
            if (InterestedVessels.Contains(vessel))
            {
                InterestedVessels.Remove(vessel);
            }
        }

        /// <summary>
        /// Process the module handlers for a protovessel we are interested in.
        /// </summary>
        /// <param name="vessel"></param>
        private void ProcessInterestedModules(InterestedVessel vessel)
        {
            if (vessel.vessel.loaded) //If vessel is loaded don't process resources.
            {
                return;
            }
            for (int i = 0; i < vessel.ModuleHandlers.Count; i++)
            {
                vessel.ModuleHandlers[i].ProcessHandler();       
            }
        }

        /// <summary>
        /// Updates the Vessel ResourceCache Overflow
        /// </summary>
        /// <param name="vessel"></param>
        private void UpdateResourceCacheOverflows(InterestedVessel vessel)
        {
            if (vessel.vessel.loaded)
            {
                vessel.ClearCaches();
            }
            else
            {
                vessel.UpdateCaches();
            }
        }
    }
}
