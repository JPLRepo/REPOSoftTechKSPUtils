using System.Collections.Generic;
using RSTUtils;
using UnityEngine;
using System;

namespace BackgroundResources
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class UnloadedResources :ScenarioModule
    {
        public static UnloadedResources Instance;
        public static DictionaryValueList<ProtoVessel, InterestedVessel> InterestedVessels;
        public static List<string> InterestingModules;
        internal bool BackgroundProcessingInstalled = false;
        private bool loggedBackgroundProcessing = false;
        private bool gamePaused = false;
        public const string configNodeName = "BACKGROUNDRESOURCES";

        /// <summary>
        /// Awake method will setup the InterestingModules that this mod will generate ElectricCharge for.
        /// Checks if the BackgroundProcessing Mod is installed and if it is this mod will not generate ElectricCharge.
        /// </summary>
        public UnloadedResources()
        {
            Instance = this;
        }

        public override void OnAwake()
        {
            Utilities.Log("OnAwake in " + HighLogic.LoadedScene);
            base.OnAwake();
            InterestedVessels = new DictionaryValueList<ProtoVessel, InterestedVessel>();
            InterestingModules = new List<string>();
            InterestingModules.Add("ModuleDeployableSolarPanel");
            InterestingModules.Add("ModuleGenerator");
            InterestingModules.Add("KopernicusSolarPanel");
            InterestingModules.Add("FissionGenerator");
            InterestingModules.Add("TacGenericConverter");
            InterestingModules.Add("ModuleResourceConverter");
            BackgroundProcessingInstalled = Utilities.IsModInstalled("BackgroundProcessing");           
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
            Utilities.Log("BackgroundProcessed Awake");
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
                        UpdateResourceCacheOverflows(vslenumerator.Current.Value);
                        ProcessInterestedModules(vslenumerator.Current.Value);
                    }
                }
            }
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            if (gameNode.HasNode(configNodeName))
            {
                ConfigNode settingsNode = gameNode.GetNode(configNodeName);
                InterestedVessels.Clear();
                var vesselNodes = settingsNode.GetNodes(InterestedVessel.configNodeName);
                foreach (ConfigNode vesselNode in vesselNodes)
                {
                    InterestedVessel interestedVessel = InterestedVessel.Load(vesselNode);
                    if (interestedVessel != null)
                    {
                        ProtoVessel key = interestedVessel.protovessel;
                        InterestedVessels.Add(key, interestedVessel);
                    }                    
                }
            }
            Utilities.Log("OnLoad: ", gameNode);
        }

        public override void OnSave(ConfigNode gameNode)
        {
            base.OnSave(gameNode);
            ConfigNode settingsNode;
            if (gameNode.HasNode(configNodeName))
            {
                settingsNode = gameNode.GetNode(configNodeName);
            }
            else
            {
                settingsNode = gameNode.AddNode(configNodeName);
            }
            Dictionary<ProtoVessel, InterestedVessel>.Enumerator vslenumerator = InterestedVessels.GetDictEnumerator();
            while (vslenumerator.MoveNext())
            {
                ConfigNode vesselNode = vslenumerator.Current.Value.Save(settingsNode);                
            }
            Utilities.Log("OnSave: ", gameNode);
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
            //if (vessel.vessel.loaded)
            //{
            //    vessel.ClearCaches();
            //}
            //else
            //{
                vessel.UpdateCaches();
            //}
        }
    }
}
