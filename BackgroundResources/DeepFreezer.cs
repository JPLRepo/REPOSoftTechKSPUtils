// Parts of this code are:
// Copyright (c) 2014 James Picone
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;


namespace BackgroundResources
{
    class DeepFreezer : SnapshotModuleHandler
    {
        internal ScreenMessage OnGoingECMsg;
        internal float deathRoll = 240f;

        public DeepFreezer(ConfigNode node, InterestedVessel vessel, ProtoPartModuleSnapshot modulesnapshot, ProtoPartSnapshot partsnapshot)
        {
            if (node == null)
            {
                Debug.Log("[UnloadedResources]: SolarPanel - Call to constructor failed. node is null.");
                return;
            }
            if (vessel == null)
            {
                Debug.Log("[UnloadedResources]: SolarPanel - Call to constructor failed. vessel is null.");
                return;
            }
            if (modulesnapshot == null)
            {
                Debug.Log("[UnloadedResources]: SolarPanel - Call to constructor failed. modulesnapshot is null.");
                return;
            }
            if (partsnapshot == null)
            {
                Debug.Log("[UnloadedResources]: SolarPanel - Call to constructor failed. partsnapshot is null.");
                return;
            }
            this.vessel = vessel;
            this.PartModule = modulesnapshot;
            this.ProtoPart = partsnapshot;
        }

        

        public override void ProcessHandler()
        {
            base.ProcessHandler();
            //Calculate EC here.
            if (Time.timeSinceLevelLoad < 2.0f || CheatOptions.InfiniteElectricity) // Check not loading level
            {
                return;
            }

            if (!DFWrapper.APIReady)
            {
                return;
            }
           
            // If the user does not have ECreqdForFreezer option ON, then we do nothing and return
            if (!DFWrapper.DeepFreezeAPI.ECReqd)
            {
                //if (debug) Debug.Log("FixedBackgroundUpdate ECreqdForFreezer is OFF, nothing to do");
                return;
            }
            // If the vessel this module is attached to is NOT stored in the DeepFreeze dictionary of known deepfreeze vessels we can't do anything, But this should NEVER happen.
            DFWrapper.VesselInfo vslinfo;
            Dictionary<Guid, DFWrapper.VesselInfo> knownVessels = DFWrapper.DeepFreezeAPI.KnownVessels;
            if (!knownVessels.TryGetValue(vessel.vessel.id, out vslinfo))
            {
                //Debug.Log("[UnloadedResources]: DeepFreezer unknown vessel, cannot process");
                return;
            }
            //Except if there are no frozen crew on board we don't need to consume any EC
            if (vslinfo.numFrozenCrew == 0)
            {
                //if (debug) Debug.Log("FixedBackgroundUpdate No Frozen Crew on-board, nothing to do");
                return;
            }
            DFWrapper.PartInfo partInfo;
            Dictionary<uint, DFWrapper.PartInfo> knownParts = DFWrapper.DeepFreezeAPI.KnownFreezerParts;
            if (!knownParts.TryGetValue(ProtoPart.flightID, out partInfo))
            {
                //Debug.Log("FixedBackgroundUpdate Can't get the Freezer Part Information, so cannot process");
                return;
            }
            // OK now we have something to do for real.
            // Calculate the time since last consumption of EC, then calculate the EC required and request it from BackgroundProcessing DLL.
            // If the vessel runs out of EC the DeepFreezeGUI class will handle notifying the user, not here.
            double currenttime = Planetarium.GetUniversalTime();
            
            double timeperiod = currenttime - partInfo.timeLastElectricity;
            if (timeperiod >= 1f && partInfo.numFrznCrew > 0) //We have frozen Kerbals, consume EC
            {
                double Ecreqd = partInfo.frznChargeRequired / 60.0f * timeperiod * vslinfo.numFrozenCrew * TimeWarp.fixedDeltaTime;
                //Debug.Log("FixedBackgroundUpdate timeperiod = " + timeperiod + " frozenkerbals onboard part = " + vslinfo.numFrozenCrew + " ECreqd = " + Ecreqd);
                double Ecrecvd = 0f;
                UnloadedResourceProcessing.RequestResource(vessel.protovessel, "ElectricCharge", (float) Ecreqd, out Ecrecvd, true);

                //Debug.Log("Consumed Freezer EC " + Ecreqd + " units");

                if ((float)Ecrecvd >= (float)Ecreqd * 0.99f)
                {
                    if (OnGoingECMsg != null) ScreenMessages.RemoveMessage(OnGoingECMsg);
                    partInfo.timeLastElectricity = (float)currenttime;
                    partInfo.deathCounter = currenttime;
                    partInfo.outofEC = false;
                    partInfo.ECWarning = false;
                    vslinfo.storedEC -= Ecrecvd;
                }
                else
                {
                    //Debug.Log("FixedBackgroundUpdate DeepFreezer Ran out of EC to run the freezer");
                    if (!partInfo.ECWarning)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00072"), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00072 = Insufficient electric charge to monitor frozen kerbals.
                        partInfo.ECWarning = true;
                        partInfo.deathCounter = currenttime;
                    }
                    if (OnGoingECMsg != null) ScreenMessages.RemoveMessage(OnGoingECMsg);
                    OnGoingECMsg = ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00073", (deathRoll - (currenttime - partInfo.deathCounter)).ToString("######0"))); //#autoLOC_DF_00073 = \u0020Freezer Out of EC : Systems critical in <<1>> secs
                    partInfo.outofEC = true;
                    //Debug.Log("FixedBackgroundUpdate deathCounter = " + partInfo.deathCounter);
                    if (currenttime - partInfo.deathCounter > deathRoll)
                    {
                        if (DFWrapper.DeepFreezeAPI.DeathFatal)
                        {
                            //Debug.Log("FixedBackgroundUpdate deathRoll reached, Kerbals all die...");
                            partInfo.deathCounter = currenttime;
                            //all kerbals dies
                            var kerbalsToDelete = new List<string>();
                            foreach (KeyValuePair<string, DFWrapper.KerbalInfo> kerbal in DFWrapper.DeepFreezeAPI.FrozenKerbals)
                            {
                                if (kerbal.Value.partID == ProtoPart.flightID && kerbal.Value.vesselID == vessel.vessel.id && kerbal.Value.type != ProtoCrewMember.KerbalType.Tourist)
                                {
                                    kerbalsToDelete.Add(kerbal.Key);
                                }
                            }
                            foreach (string deathKerbal in kerbalsToDelete)
                            {
                                DFWrapper.DeepFreezeAPI.KillFrozenCrew(deathKerbal);
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_DF_00074", deathKerbal), 10.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_DF_00074 = <<1>> died due to lack of Electrical Charge to run cryogenics
                                //Debug.Log("FixedBackgroundUpdate DeepFreezer - kerbal " + deathKerbal + " died due to lack of Electrical charge to run cryogenics");
                            }
                            //kerbalsToDelete.ForEach(id => DFWrapper.DeepFreezeAPI.FrozenKerbals.Remove(id));
                        }
                        else //NON Fatal option - emergency thaw all kerbals.
                        {
                            // Cannot emergency thaw in background processing. It is expected that DeepFreezeGUI will pick up that EC has run out and prompt the user to switch to the vessel.
                            // When the user switches to the vessel the DeepFreezer partmodule will detect no EC is available and perform an emergency thaw procedure.
                            //Debug.Log("FixedBackgroundUpdate DeepFreezer - EC has run out non-fatal option");
                        }
                    }
                }
            }
        }
    }
}
