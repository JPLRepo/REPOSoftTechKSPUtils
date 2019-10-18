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
using UnityEngine;


namespace BackgroundResources
{
    class SolarPanel : SnapshotModuleHandler
    {
        public ModuleDeployableSolarPanel.PanelType panelType = ModuleDeployableSolarPanel.PanelType.FLAT;
        public float chargeRate = 1f;
        public Vector3d position;
        public Quaternion orientation;
        public FloatCurve powerCurve;
        public float sunAOA;
        public float flowRate;
        public float flowMult = 1f;
        public float efficiencyMult = 1f;
        public bool sunTracking;
        public Vector3d solarNormal;
        public Vector3d pivotAxis;
        public bool usesCurve;
        public FloatCurve tempCurve;
        public float temperature;
        public ModuleDeployablePart.DeployState deployState = ModuleDeployablePart.DeployState.RETRACTED;

        public SolarPanel(ConfigNode node, InterestedVessel vessel, ProtoPartModuleSnapshot modulesnapshot, ProtoPartSnapshot partsnapshot)
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
            
            if (node.HasValue("type"))
            {
                panelType = (ModuleDeployableSolarPanel.PanelType)System.Enum.Parse(typeof(ModuleDeployableSolarPanel.PanelType), node.GetValue("type"));
            }
            node.TryGetValue("chargeRate", ref chargeRate);
            node.TryGetValue("sunAOA", ref this.sunAOA);
            node.TryGetValue("flowRate", ref this.flowRate);
            node.TryGetValue("flowMult", ref this.flowMult);
            node.TryGetValue("efficiencyMult", ref this.efficiencyMult);
            if (node.HasValue("deployState"))
            {
                deployState = (ModuleDeployablePart.DeployState) System.Enum.Parse(typeof(ModuleDeployablePart.DeployState),node.GetValue("deployState"));
            }
            if (deployState != ModuleDeployablePart.DeployState.EXTENDED)
            {
                sunAOA = 0f;
                flowRate = 0f;
            }
            Part part = PartLoader.getPartInfoByName(partsnapshot.partName).partPrefab;
            if (part == null)
            {
                Debug.Log("[UnloadedResources]: SolarPanel - Unable to Find Part: " + partsnapshot.partName);
                return;
            }
            for (int i = 0; i < part.Modules.Count; ++i)
            {
                if (part.Modules[i].moduleName == "ModuleDeployableSolarPanel" || part.Modules[i].moduleName == "KopernicusSolarPanel")
                {
                    ModuleDeployableSolarPanel panelModule = (ModuleDeployableSolarPanel) part.Modules[i];
                    solarNormal = panelModule.part.FindModelTransform(panelModule.secondaryTransformName).forward;
                    pivotAxis = panelModule.part.FindModelTransform(panelModule.pivotName).up;
                    sunTracking = panelModule.isTracking && panelModule.trackingMode == ModuleDeployablePart.TrackingMode.SUN;
                    usesCurve = panelModule.useCurve;
                    tempCurve = panelModule.temperatureEfficCurve;
                    temperature = (float) partsnapshot.temperature;
                    powerCurve = panelModule.powerCurve;
                    position = partsnapshot.position;
                    orientation = partsnapshot.rotation;
                    break;
                }
            }
            this.moduleType = UnloadedResources.ModuleType.Producer;
        }

        private bool Raytrace(Vector3d p, Vector3d dir, CelestialBody body)
        {
            // ray from origin to body center
            Vector3d diff = body.position - p;
            // projection of origin->body center ray over the raytracing direction
            double k = Vector3d.Dot(diff, dir);
            // the ray doesn't hit body if its minimal analytical distance along the ray is less than its radius
            return k < 0.0 || (dir * k - diff).magnitude > body.Radius;
        }

        private bool RaytraceBody(Vessel vessel, CelestialBody body, out Vector3d dir, out double dist)
        {
            // generate ray parameters
            Vector3d vessel_pos = VesselPosition(vessel);
            dir = body.position - vessel_pos;
            dist = dir.magnitude;
            dir /= dist;
            dist -= body.Radius;
            // raytrace
            return (body == vessel.mainBody || Raytrace(vessel_pos, dir, vessel.mainBody))
                   && (body == vessel.mainBody.referenceBody || vessel.mainBody.referenceBody == null || Raytrace(vessel_pos, dir, vessel.mainBody.referenceBody));
        }

        private Vector3d VesselPosition(Vessel v)
        {
            // the issue
            //   - GetWorldPos3D() return mainBody position for a few ticks after scene changes
            //   - we can detect that, and fall back to evaluating position from the orbit
            //   - orbit is not valid if the vessel is landed, and for a tick on prelauch/staging/decoupling
            //   - evaluating position from latitude/longitude work in all cases, but is probably the slowest method
            Vector3d pos = v.GetWorldPos3D();
            // during scene changes, it will return mainBody position
            if (Vector3d.SqrMagnitude(pos - v.mainBody.position) < 1.0)
            {
                // try to get it from orbit
                pos = v.orbit.getPositionAtUT(Planetarium.GetUniversalTime());
                // if the orbit is invalid (landed, or 1 tick after prelauch/staging/decoupling)
                if (double.IsNaN(pos.x))
                {
                    // get it from lat/long (work even if it isn't landed)
                    pos = v.mainBody.GetWorldSurfacePosition(v.latitude, v.longitude, v.altitude);
                }
            }
            return pos;
        }

        private double SolarLuminosity
        {
            get
            {
                // note: it is 0 before loading first vessel in a game session, we compute it in that case
                if (PhysicsGlobals.SolarLuminosity <= Double.Epsilon)
                {
                    double semiMajorAxis = FlightGlobals.GetHomeBody().orbit.semiMajorAxis;
                    return semiMajorAxis * semiMajorAxis * 12.566370614359172 * PhysicsGlobals.SolarLuminosityAtHome;
                }
                return PhysicsGlobals.SolarLuminosity;
            }
        }

        public override void ProcessHandler()
        {
            base.ProcessHandler();
            //Calculate EC here.
            Vector3d sun_dir;
            double sun_dist;
            bool in_sunlight = RaytraceBody(vessel.vessel, FlightGlobals.Bodies[0], out sun_dir, out sun_dist);
            Vector3d partPos = VesselPosition(vessel.vessel) + position;

            double orientationFactor = 1;
            if (sunTracking)
            {
                Vector3d localPivot = (vessel.vessel.transform.rotation * orientation * pivotAxis).normalized;
                orientationFactor = Math.Cos(Math.PI / 2.0 - Math.Acos(Vector3d.Dot(localPivot, sun_dir)));
            }
            else
            {
                Vector3d localSolarNormal = (vessel.vessel.transform.rotation * orientation * solarNormal).normalized;
                orientationFactor = Vector3d.Dot(localSolarNormal, sun_dir);
            }

            orientationFactor = Math.Max(orientationFactor, 0);

            if (in_sunlight)
            {
                double solarFlux = SolarLuminosity / (12.566370614359172 * sun_dist * sun_dist);
                
                double staticPressure = vessel.vessel.mainBody.GetPressure(vessel.vessel.altitude);
                
                if (staticPressure > 0.0)
                {
                    double density = vessel.vessel.mainBody.GetDensity(staticPressure, temperature);
                    Vector3 up = FlightGlobals.getUpAxis(vessel.vessel.mainBody, vessel.vessel.vesselTransform.position).normalized;
                    double sunPower = vessel.vessel.mainBody.radiusAtmoFactor * Vector3d.Dot(up, sun_dir);
                    double sMult = vessel.vessel.mainBody.GetSolarPowerFactor(density);
                    if (sunPower < 0)
                    {
                        sMult /= Math.Sqrt(2.0 * vessel.vessel.mainBody.radiusAtmoFactor + 1.0);
                    }
                    else
                    {
                        sMult /= Math.Sqrt(sunPower * sunPower + 2.0 * vessel.vessel.mainBody.radiusAtmoFactor + 1.0) - sunPower;
                    }
                    solarFlux *= sMult;
                    
                }
                
                float multiplier = 1;
                if (usesCurve) { multiplier = powerCurve.Evaluate((float)FlightGlobals.Bodies[0].GetAltitude(partPos)); }
                else { multiplier = (float)(solarFlux / PhysicsGlobals.SolarLuminosityAtHome); }

                float tempFactor = tempCurve.Evaluate(temperature);
                float resourceAmount = chargeRate * (float)orientationFactor * tempFactor * multiplier;
                double amtReceived = 0f;
                UnloadedResourceProcessing.RequestResource(vessel.protovessel, "ElectricCharge", resourceAmount * TimeWarp.fixedDeltaTime, out amtReceived, true);
            }
        }
    }
}
