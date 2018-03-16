using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackgroundResources
{
    class FuelCell : SnapshotModuleHandler
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
               RSTUtils.Utilities.Log("FuelCell: Error creating recipe");
            }
            return recipe;
        }

        public FuelCell(ConfigNode node, InterestedVessel vessel, ProtoPartModuleSnapshot modulesnapshot, ProtoPartSnapshot partsnapshot)
        {
            this.vessel = vessel;
            this.PartModule = modulesnapshot;
            node.TryGetValue("IsActivated", ref converterIsActive);
            int count = node.CountNodes;
            for (int i = 0; i < count; ++i)
            {
                ConfigNode subNode = node.nodes[i];
                ResourceRatio newResource = new ResourceRatio() { FlowMode = ResourceFlowMode.NULL };
                newResource.Load(subNode);
                switch (subNode.name)
                {
                    case "INPUT_RESOURCE":
                        inputResList.Add(newResource);
                        break;
                    case "OUTPUT_RESOURCE":
                        outputResList.Add(newResource);
                        break;
                    case "REQUIRED_RESOURCE":
                        requiredResList.Add(newResource);
                        break;
                }
            }
        }

        public override void ProcessHandler()
        {
            if (converterIsActive)
            {
                base.ProcessHandler();
                double amtReceived = 0f;
                //UnloadedResourceProcessing.RequestResource(vessel.protovessel, "ElectricCharge", rate * TimeWarp.fixedDeltaTime, out amtReceived, true);
            }
        }
    }
}
