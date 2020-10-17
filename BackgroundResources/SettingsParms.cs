using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace BackgroundResources
{
    public class BackgroundResources_SettingsParms : GameParameters.CustomParameterNode

    {
        public override string Title { get { return Localizer.Format("BackgroundResources Options"); } } //#autoLOC_DF_00144 = DeepFreeze Options
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return true; } }
        public override string Section { get { return "BackgroundResources"; } }
        public override string DisplaySection { get { return Localizer.Format("BackgroundResources"); } } //#autoLOC_DF_00003 = DeepFreeze
        public override int SectionOrder { get { return 1; } }

        
        [GameParameters.CustomParameterUI("Unloaded Vessel Processing", autoPersistance = true, toolTip = "If enabled BackgroundResources will process resources on unloaded vessels. If disabled, it won't (does nothing).")] //#autoLOC_DF_00201 = Unloaded Vessel Processing  //#autoLOC_DF_00202 = If enabled DeepFreeze will process resources on unloaded vessels. If disabled, it won't and play the catchup and estimation game.
        public bool backgroundresources = true;
        
        [GameParameters.CustomParameterUI("Produce Resources", autoPersistance = true, toolTip = "If enabled BackgroundResources will produce resources on unloaded vessels. If disabled, it won't.")] //#autoLOC_DF_00147 = Fatal EC/Heat Option #autoLOC_DF_00148 = If on Kerbals will die if EC runs out or it gets too hot
        public bool ProduceResources = true;

        [GameParameters.CustomIntParameterUI("Consume Resources", autoPersistance = true, toolTip = "If enabled BackgroundResources will consume resources on unloaded vessels. If disabled, it won't.")] //#autoLOC_DF_00149 = Non Fatal Comatose Time(in secs) #autoLOC_DF_00150 = The time in seconds a kerbal is comatose\n if fatal EC / Heat option is off
        public bool ConsumeResources = true;

        [GameParameters.CustomIntParameterUI("Include EC Generating Resource Converters", autoPersistance = true, toolTip = "If enabled BackgroundResources will include parts that generate EC that use the stock Generic Resource Converter on unloaded vessels. If disabled, it won't.")] //#autoLOC_DF_00149 = Non Fatal Comatose Time(in secs) #autoLOC_DF_00150 = The time in seconds a kerbal is comatose\n if fatal EC / Heat option is off
        public bool IncludeGenericResourceConverters = false;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Debug.Log("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    this.backgroundresources = true;
                    this.ProduceResources = true;
                    this.ConsumeResources = true;
                    this.IncludeGenericResourceConverters = false;
                    break;
                case GameParameters.Preset.Normal:
                    this.backgroundresources = true;
                    this.ProduceResources = true;
                    this.ConsumeResources = true;
                    this.IncludeGenericResourceConverters = false;
                    break;
                case GameParameters.Preset.Moderate:
                    this.backgroundresources = true;
                    this.ProduceResources = true;
                    this.ConsumeResources = true;
                    this.IncludeGenericResourceConverters = false;
                    break;
                case GameParameters.Preset.Hard:
                    this.backgroundresources = true;
                    this.ProduceResources = true;
                    this.ConsumeResources = true;
                    this.IncludeGenericResourceConverters = false;
                    break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (HighLogic.fetch != null)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (HighLogic.fetch != null)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    return false;
                }
            }

            if (member.Name == "ProduceResources")
            {
                return parameters.CustomParams<BackgroundResources_SettingsParms>().backgroundresources;
            }
            if (member.Name == "ConsumeResources")
            {
                return parameters.CustomParams<BackgroundResources_SettingsParms>().backgroundresources;
            }
            if (member.Name == "IncludeGenericResourceConverters")
            {
                return parameters.CustomParams<BackgroundResources_SettingsParms>().backgroundresources;
            }
            return true;
        }
    }    
}
