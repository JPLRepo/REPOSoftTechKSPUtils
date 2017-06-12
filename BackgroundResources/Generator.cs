using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackgroundResources
{
    class Generator : SnapshotModuleHandler
    {
        public bool generatorIsActive = true;
        public float efficiency = 0f;
        public float rate = 0f;

        public Generator(ConfigNode node, InterestedVessel vessel, ProtoPartModuleSnapshot modulesnapshot, ProtoPartSnapshot partsnapshot)
        {
            this.vessel = vessel;
            this.PartModule = modulesnapshot;
            node.TryGetValue("generatorIsActive", ref generatorIsActive);
            ConfigNode[] modulenodes = partsnapshot.partInfo.partConfig.GetNodes();
            for (int i = 0; i < modulenodes.Length; i++)
            {
                ConfigNode resNode = new ConfigNode();
                if (modulenodes[i].TryGetNode("OUTPUT_RESOURCE", ref resNode))
                {
                    string resName = string.Empty;
                    resNode.TryGetValue("name", ref resName);
                    if (resName == "ElectricCharge")
                    {
                        resNode.TryGetValue("rate", ref rate);
                    }
                }
            }
        }

        public override void ProcessHandler()
        {
            if (generatorIsActive)
            {
                base.ProcessHandler();
                efficiency = 1f;
                double amtReceived = 0f; 
                UnloadedResourceProcessing.RequestResource(vessel.protovessel, "ElectricCharge", rate * TimeWarp.fixedDeltaTime, out amtReceived, true);
            }
        }
    }
}
