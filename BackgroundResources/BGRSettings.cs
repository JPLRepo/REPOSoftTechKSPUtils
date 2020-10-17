using System;
using System.Collections.Generic;
using RSTUtils;

namespace BackgroundResources
{
    public class BGRSettings
    {
        // this class stores the DeepFreeze Settings from the config file.
        private const string configNodeName = "BGRSettings";
        public bool backgroundresources;
        public bool ProduceResources;
        public bool ConsumeResources;
        public bool IncludeGenericResourceConverters;


        public BGRSettings()
        {
            backgroundresources = true;
            ProduceResources = true;
            ConsumeResources = true;
            IncludeGenericResourceConverters = false;
        }

        //Settings Functions Follow

        internal void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode BGRsettingsNode = node.GetNode(configNodeName);

                BGRsettingsNode.TryGetValue("backgroundresources", ref backgroundresources);
                BGRsettingsNode.TryGetValue("ProduceResources", ref ProduceResources);
                BGRsettingsNode.TryGetValue("ConsumeResources", ref ConsumeResources);
                BGRsettingsNode.TryGetValue("IncludeGenericResourceConverters", ref IncludeGenericResourceConverters);
                
                ApplySettings();
            }
        }

        internal void Save(ConfigNode node)
        {
            ConfigNode settingsNode;
            if (node.HasNode(configNodeName))
            {
                settingsNode = node.GetNode(configNodeName);
                settingsNode.ClearData();
            }
            else
            {
                settingsNode = node.AddNode(configNodeName);
            }

            settingsNode.AddValue("backgroundresources", backgroundresources);
            settingsNode.AddValue("ProduceResources", ProduceResources);
            settingsNode.AddValue("ConsumeResources", ConsumeResources);
            settingsNode.AddValue("IncludeGenericResourceConverters", IncludeGenericResourceConverters);
            Utilities.Log_Debug("BGRSettings save complete");
        }

        internal void ApplySettings()
        {
            Utilities.Log_Debug("BGRSettings ApplySettings Start");
            if (HighLogic.CurrentGame != null)
            {
                var BGR_SettingsParms = HighLogic.CurrentGame.Parameters.CustomParams<BackgroundResources_SettingsParms>();
                if (BGR_SettingsParms != null)
                {
                    if (UnloadedResources.Instance != null)
                    {
                        UnloadedResources.Instance.bgrSettings.backgroundresources = BGR_SettingsParms.backgroundresources;
                        UnloadedResources.Instance.bgrSettings.ConsumeResources = BGR_SettingsParms.ConsumeResources;
                        UnloadedResources.Instance.bgrSettings.ProduceResources = BGR_SettingsParms.ProduceResources;
                        UnloadedResources.Instance.bgrSettings.IncludeGenericResourceConverters = BGR_SettingsParms.IncludeGenericResourceConverters;
                    }                    
                }
                else
                    Utilities.Log_Debug("BGRSettings ApplySettings Settings Params Not Set!");
            }
            else
                Utilities.Log_Debug("BGRSettings ApplySettings CurrentGame is NULL!");
            Utilities.Log_Debug("BGRSettings ApplySettings End");
        }
    }
}