using System;
using System.Collections.Generic;

namespace BackgroundResources
{
    class ModuleResourceConverter : SnapshotModuleHandler
    {
        public bool converterIsActive = true;
        public List<ResourceRatio> inputResList;
        public List<ResourceRatio> outputResList;
        public List<ResourceRatio> requiredResList;
        
        private ResourceConverter _resConverter;
        public ResourceConverter ResConverter
        {
            get
            {
                if (_resConverter == null)
                {
                    _resConverter = new ResourceConverter(ResBroker);
                }
                return _resConverter;
            }
        }

        private IResourceBroker _resBroker;
        public IResourceBroker ResBroker
        {
            get
            {
                if (_resBroker == null)
                {
                    _resBroker = new ResourceBroker();
                }
                return _resBroker;
            }
        }

        private ConversionRecipe _recipe;
        public virtual ConversionRecipe Recipe
        {
            get
            {
                if (_recipe == null)
                {
                    _recipe = GenerateRecipe();
                }
                return _recipe;
            }
        }

        private ConversionRecipe GenerateRecipe()
        {
            ConversionRecipe recipe = new ConversionRecipe();
            try
            {
                recipe.Inputs.AddRange(inputResList);
                recipe.Outputs.AddRange(outputResList);
                recipe.Requirements.AddRange(requiredResList);
            }
            catch (Exception)
            {
               RSTUtils.Utilities.Log("ModuleResourceConverter: Error creating recipe");
            }
            return recipe;
        }

        public static bool ResourceConverterGeneratesEC(ProtoPartSnapshot partsnapshot)
        {
            if (partsnapshot != null && partsnapshot.partPrefab != null)
            {
                for (int i = 0; i < partsnapshot.partPrefab.Modules.Count; i++)
                {
                    BaseConverter converter = partsnapshot.partPrefab.Modules[i] as BaseConverter;
                    bool tacConverter = partsnapshot.partPrefab.Modules[i].ClassID == "Tac.TacGenericConverter".GetHashCode();
                    if (converter != null && tacConverter)
                    {
                        for (int j = 0; j < converter.outputList.Count; j++)
                        {
                            if (converter.outputList[j].ResourceName == "ElectricCharge")
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public ModuleResourceConverter(ConfigNode node, InterestedVessel vessel, ProtoPartModuleSnapshot modulesnapshot, ProtoPartSnapshot partsnapshot)
        {
            this.vessel = vessel;
            this.PartModule = modulesnapshot;
            node.TryGetValue("IsActivated", ref converterIsActive);
            if (partsnapshot != null && partsnapshot.partPrefab != null)
            {
                for (int i = 0; i < partsnapshot.partPrefab.Modules.Count; i++)
                {
                    BaseConverter converter = partsnapshot.partPrefab.Modules[i] as BaseConverter;
                    bool tacConverter = partsnapshot.partPrefab.Modules[i].ClassID == "Tac.TacGenericConverter".GetHashCode();
                    if (converter != null && tacConverter)
                    {
                        inputResList = converter.inputList;
                        outputResList = converter.outputList;
                        requiredResList = converter.reqList;
                    }
                }
            }
            this.moduleType = UnloadedResources.ModuleType.Both;
        }

        public override void ProcessHandler()
        {
            if (converterIsActive)
            {
                base.ProcessHandler();
                double amtRequired = 0f;
                double amtReceived = 0f;
                bool inputsReceived = true;
                for (int i = 0; i < inputResList.Count; i++)
                {
                    if (inputResList[i].ResourceName == "IntakeAir")
                    {
                        if (vessel.vessel.staticPressurekPa > 0d && vessel.vessel.mainBody.atmosphereContainsOxygen)
                        {
                            //Assume we have enough Air...
                            continue;
                        }
                        else
                        {
                            //RSTUtils.Utilities.Log("TACGenericConverter: Failed to Get Air Resource Vessel " + vessel.vessel.vesselName);
                            inputsReceived = false;
                            break;
                        }
                    }
                    amtRequired = inputResList[i].Ratio * TimeWarp.fixedDeltaTime;
                    UnloadedResourceProcessing.RequestResource(vessel.protovessel, inputResList[i].ResourceName, amtRequired, out amtReceived);
                    //RSTUtils.Utilities.Log("TACGenericConverter: Requested Input Resource " + inputResList[i].ResourceName + " Amount:" + amtReceived);
                    if (amtReceived < amtRequired)
                    {
                        //RSTUtils.Utilities.Log("TACGenericConverter: Failed to Get required Resource " + inputResList[i].ResourceName);
                        inputsReceived = false;
                        break;
                    }
                }
                if (inputsReceived)
                {
                    for (int i = 0; i < outputResList.Count; i++)
                    {
                        amtRequired = outputResList[i].Ratio * TimeWarp.fixedDeltaTime;
                        UnloadedResourceProcessing.RequestResource(vessel.protovessel, outputResList[i].ResourceName, amtRequired, out amtReceived, true);
                        //RSTUtils.Utilities.Log("TACGenericConverter: Generated Output Resource " + outputResList[i].ResourceName + " Amount:" + amtReceived);
                    }
                }
            }
        }
    }
}
