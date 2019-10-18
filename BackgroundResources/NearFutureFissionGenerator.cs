namespace BackgroundResources
{
    class NearFutureFissionGenerator : SnapshotModuleHandler
    {
        public float generationRate;
        public bool isEnabled;
        
        public NearFutureFissionGenerator(ConfigNode node, InterestedVessel vessel, ProtoPartModuleSnapshot modulesnapshot, ProtoPartSnapshot partsnapshot)
        {
            this.vessel = vessel;
            PartModule = modulesnapshot;
            isEnabled = false;
            generationRate = 0f;
            node.TryGetValue("CurrentGeneration", ref generationRate);
            node.TryGetValue("isEnabled", ref isEnabled);
            this.moduleType = UnloadedResources.ModuleType.Producer;
        }

        public override void ProcessHandler()
        {
            if (isEnabled)
            {
                base.ProcessHandler();
                double amtReceived = 0f; 
                UnloadedResourceProcessing.RequestResource(vessel.protovessel, "ElectricCharge", generationRate * TimeWarp.fixedDeltaTime, out amtReceived, true);
            }
        }
    }
}
