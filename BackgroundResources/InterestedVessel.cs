using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BackgroundResources
{
    public class SnapshotModuleHandler : IEquatable<ProtoPartModuleSnapshot>
    {
        public ProtoPartModuleSnapshot PartModule;
        public InterestedVessel vessel;

        public bool Equals(ProtoPartModuleSnapshot module)
        {
            return this.PartModule == module;
        }

        public virtual void ProcessHandler()
        { }
    }

    public static class ListExtension
    {
        public static bool ContainsModule(this List<SnapshotModuleHandler> list, ProtoPartModuleSnapshot item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].PartModule == item)
                {
                    return true;
                }
            }
            return false;
        }
    }
    public class InterestedVessel
    {
        public Vessel vessel;
        public ProtoVessel protovessel;
        public List<SnapshotModuleHandler> ModuleHandlers;
        public List<CacheResources.CacheResource> CachedResources;
        public double TimeLastRefresh;
        //public List<ProtoPartModuleSnapshot> PartModules;
        

        public InterestedVessel(Vessel vessel, ProtoVessel protovessel)
        {
            this.vessel = vessel;
            this.protovessel = protovessel;
            this.TimeLastRefresh = Time.time;
            ModuleHandlers = new List<SnapshotModuleHandler>();
            CachedResources = new List<CacheResources.CacheResource>();
            UpdateModules();
        }

        public void UpdateModules()
        {
            for (int i = 0; i < protovessel.protoPartSnapshots.Count; i++)
            {
                ProtoPartSnapshot partsnapshot = protovessel.protoPartSnapshots[i];
                
                for (int j = 0; j < partsnapshot.modules.Count; j++)
                {
                    ProtoPartModuleSnapshot modulesnapshot = partsnapshot.modules[j];
                    for (int k = 0; k < UnloadedResources.InterestingModules.Count; k++)
                    {
                        if (modulesnapshot.moduleName == UnloadedResources.InterestingModules[k])
                        {
                            if (!ModuleHandlers.ContainsModule(modulesnapshot))
                            {
                                if (UnloadedResources.InterestingModules[k] == "ModuleDeployableSolarPanel" || UnloadedResources.InterestingModules[k] == "KopernicusSolarPanel")
                                {
                                    ModuleHandlers.Add(new SolarPanel(modulesnapshot.moduleValues, this, modulesnapshot, partsnapshot));
                                }
                                if (UnloadedResources.InterestingModules[k] == "ModuleGenerator")
                                {
                                    ModuleHandlers.Add(new Generator(modulesnapshot.moduleValues, this, modulesnapshot, partsnapshot));
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ClearCaches()
        {
            for (int crI = 0; crI < CachedResources.Count; crI++)
            {
                CachedResources[crI].timeWarpOverflow.Clear();
            }
        }

        public void UpdateCaches()
        {
            for (int crI = 0; crI < CachedResources.Count; crI++)
            {
                CachedResources[crI].timeWarpOverflow.Update();
            }
        }
    }
}
