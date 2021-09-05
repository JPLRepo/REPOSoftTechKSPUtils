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
        public ProtoPartSnapshot ProtoPart;
        public UnloadedResources.ModuleType moduleType = UnloadedResources.ModuleType.Both;

        public bool Equals(ProtoPartModuleSnapshot module)
        {
            return this.PartModule == module;
        }

        public virtual void ProcessHandler()
        {
            if (UnloadedResources.Instance)
            {
                if (!UnloadedResources.Instance.bgrSettings.backgroundresources)
                {
                    vessel.TimeLastRefresh = Time.time;
                    return;
                }
                if (!UnloadedResources.Instance.bgrSettings.ConsumeResources && moduleType == UnloadedResources.ModuleType.Consumer)
                {
                    vessel.TimeLastRefresh = Time.time;
                    return;
                }
                else if (!UnloadedResources.Instance.bgrSettings.ProduceResources && moduleType == UnloadedResources.ModuleType.Producer)
                {
                    vessel.TimeLastRefresh = Time.time;
                    return;
                }
                else if (moduleType == UnloadedResources.ModuleType.Both && (!UnloadedResources.Instance.bgrSettings.ConsumeResources || !UnloadedResources.Instance.bgrSettings.ProduceResources))
                {
                    vessel.TimeLastRefresh = Time.time;
                    return;
                }
            }
        }
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
        public const string configNodeName = "INTERESTEDVESSEL";

        public InterestedVessel(Vessel vessel, ProtoVessel protovessel)
        {
            this.vessel = vessel;
            this.protovessel = protovessel;
            this.TimeLastRefresh = Time.time;
            ModuleHandlers = new List<SnapshotModuleHandler>();
            CachedResources = new List<CacheResources.CacheResource>();
            UpdateModules();
        }

        public ConfigNode Save(ConfigNode Savenode)
        {
            ConfigNode node = Savenode.AddNode(configNodeName);
            node.AddValue("VesselGuid", vessel.id);
            node.AddValue("timeLastRefresh", TimeLastRefresh);
            for (int crI = 0; crI < CachedResources.Count; crI++)
            {
                CachedResources[crI].Save(node);
            }
            return node;
        }

        public static InterestedVessel Load(ConfigNode node)
        {
            if (node.HasValue("VesselGuid"))
            {
                Guid id = new Guid(node.GetValue("VesselGuid"));
                if (FlightGlobals.fetch)
                {
                    Vessel vsl = FlightGlobals.FindVessel(id);
                    if (vsl != null)
                    {
                        if (vsl.protoVessel != null)
                        {
                            ProtoVessel protovsl = vsl.protoVessel;
                            InterestedVessel interestedVessel = new InterestedVessel(vsl, protovsl);
                            node.TryGetValue("timeLastRefresh", ref interestedVessel.TimeLastRefresh);
                            ConfigNode[] cacheResourcesNodes = node.GetNodes("CACHERESOURCE");
                            for (int crI = 0; crI < cacheResourcesNodes.Length; crI++)
                            {
                                CacheResources.CacheResource cacheResource = CacheResources.CacheResource.Load(cacheResourcesNodes[crI], protovsl);
                                if (cacheResource != null)
                                {
                                    interestedVessel.CachedResources.Add(cacheResource);
                                }
                            }
                            return interestedVessel;
                        }
                    }
                }                
            }
            return null;
        }

        public void UpdateModules()
        {
            for (int i = 0; i < protovessel.protoPartSnapshots.Count; i++)
            {
                ProtoPartSnapshot partsnapshot = protovessel.protoPartSnapshots[i];
                
                for (int j = 0; j < partsnapshot.modules.Count; j++)
                {
                    ProtoPartModuleSnapshot modulesnapshot = partsnapshot.modules[j];
                    if (UnloadedResources.InterestingModules.ContainsKey(modulesnapshot.moduleName))
                    {
                        if (!ModuleHandlers.ContainsModule(modulesnapshot))
                        {
                            if (modulesnapshot.moduleName == "ModuleDeployableSolarPanel" || modulesnapshot.moduleName == "KopernicusSolarPanelsFixer" || modulesnapshot.moduleName == "KopernicusSolarPanel")
                            {
                                ModuleHandlers.Add(new SolarPanel(modulesnapshot.moduleValues, this, modulesnapshot, partsnapshot));
                            }
                            if (modulesnapshot.moduleName == "ModuleGenerator")
                            {
                                ModuleHandlers.Add(new Generator(modulesnapshot.moduleValues, this, modulesnapshot, partsnapshot));
                            }
                            if (modulesnapshot.moduleName == "FissionGenerator")
                            {
                                ModuleHandlers.Add(new NearFutureFissionGenerator(modulesnapshot.moduleValues, this, modulesnapshot, partsnapshot));
                            }
                            if (modulesnapshot.moduleName == "TacGenericConverter")
                            {
                                ModuleHandlers.Add(new TacGenericConverter(modulesnapshot.moduleValues, this, modulesnapshot, partsnapshot));
                            }
                            if (modulesnapshot.moduleName == "ModuleResourceConverter" && ModuleResourceConverter.ResourceConverterGeneratesEC(partsnapshot))
                            {
                                ModuleHandlers.Add(new ModuleResourceConverter(modulesnapshot.moduleValues, this, modulesnapshot, partsnapshot));
                            }
                            if (modulesnapshot.moduleName == "DeepFreezer")
                            {
                                ModuleHandlers.Add(new DeepFreezer(modulesnapshot.moduleValues, this, modulesnapshot, partsnapshot));
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

        public static bool ContainsInterestedModules(ProtoVessel protoVessel)
        {
            for (int i = 0; i < protoVessel.protoPartSnapshots.Count; i++)
            {
                ProtoPartSnapshot partsnapshot = protoVessel.protoPartSnapshots[i];

                for (int j = 0; j < partsnapshot.modules.Count; j++)
                {
                    ProtoPartModuleSnapshot modulesnapshot = partsnapshot.modules[j];
                    if (UnloadedResources.InterestingModules.ContainsKey(modulesnapshot.moduleName))
                    {
                        if (modulesnapshot.moduleName == "ModuleDeployableSolarPanel" || modulesnapshot.moduleName == "KopernicusSolarPanel")
                        {
                            return true;
                        }
                        if (modulesnapshot.moduleName == "ModuleGenerator")
                        {
                            return true;
                        }
                        if (modulesnapshot.moduleName == "FissionGenerator")
                        {
                            return true;
                        }
                        if (modulesnapshot.moduleName == "TacGenericConverter")
                        {
                            return true;
                        }
                        if (modulesnapshot.moduleName == "ModuleResourceConverter" && ModuleResourceConverter.ResourceConverterGeneratesEC(partsnapshot))
                        {
                            return true;
                        }
                        if (modulesnapshot.moduleName == "DeepFreezer")
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
