using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSPAchievements;
using Smooth.Compare.Comparers;
using Smooth.Slinq.Test;
using UnityEngine;

namespace BackgroundResources
{
    #region UnloadedResourceProcessing
    /// <summary>
    /// Used to Cache Resources for ProtoVessels
    /// </summary>
    public static class CacheResources
    {
        public static int timeWarpStep = 4;

        public class CacheTimeWarpBuffer
        {
            private class CacheTimeWarpEntry
            {
                public double time;
                public double amount;

                public CacheTimeWarpEntry(double time, double amount)
                {
                    this.time = time;
                    this.amount = amount;
                }
            }
            private Queue<CacheTimeWarpEntry> bufferList;
            public double totalAmount;

            /// <summary>
            /// Create a new CacheTimeWarpBuffer
            /// </summary>
            public CacheTimeWarpBuffer()
            {
                bufferList = new Queue<CacheTimeWarpEntry>();
                totalAmount = 0;
            }

            /// <summary>
            /// Updates the Buffer. Call Every FixedUpdate. Checks if Timewarp rate less than 3, throw away the buffer entries.
            /// Will remove any buffer entries older than fixedDeltaTime * 2 (two ticks).
            /// </summary>
            public void Update()
            {
                if (TimeWarp.CurrentRateIndex < timeWarpStep)  //If timewarp rate is less than three throw away the queue and we are done.
                {
                    Clear();
                    return;
                }
                //Otherwise throw away anything more than 2 ticks ago from the queue.
                double oldTime = Planetarium.GetUniversalTime() - (TimeWarp.fixedDeltaTime * 2);
                int dequeueCount = 0;
                foreach(CacheTimeWarpEntry entry in bufferList)
                {
                    if (entry.time < oldTime)
                    {
                        dequeueCount++;
                    }
                }
                for (int dqI = 0; dqI < dequeueCount; dqI++)
                {
                    CacheTimeWarpEntry entry = bufferList.Dequeue();
                    totalAmount -= entry.amount;
                }
            }

            /// <summary>
            /// Clear the Buffer
            /// </summary>
            public void Clear()
            {
                bufferList.Clear();
                totalAmount = 0;
            }

            /// <summary>
            /// Add to the TimeWarp Buffer
            /// </summary>
            /// <param name="amount">The amount to add</param>
            public void Add(double amount)
            {
                double key = Planetarium.GetUniversalTime();
                CacheTimeWarpEntry newEntry = new CacheTimeWarpEntry(key, amount);
                bufferList.Enqueue(newEntry);
                totalAmount += amount;
            }

            /// <summary>
            /// Take from the TimeWarp Buffer.
            /// </summary>
            /// <param name="amount">The amount we want</param>
            /// <param name="amountTaken">out parm containing the amount taken</param>
            /// <returns>true if we got what we wanted, otherwise false</returns>
            public bool Take(double amount, out double amountTaken)
            {
                amountTaken = 0;
                while (amount > 0 && bufferList.Count > 0) //We still need amount and still on the Queue
                {
                    CacheTimeWarpEntry entry = bufferList.Dequeue(); //Get last entry
                    totalAmount -= entry.amount; //Decrease total amount
                    if (entry.amount > amount) //If we got more than we need just throw away the rest
                    {
                        entry.amount = amount;
                    }
                    amountTaken += entry.amount; //add to the amount we are giving.
                    amount -= entry.amount; //Decrease the amount we still need.
                }
                if (amount <= 0)
                    return true;
                return false;
            }
        }
        
        /// <summary>
        /// Information about a Resource and reference to it's ProtoPartResourceSnapshot.
        /// </summary>
        public class CacheResource
        {
            public string resourceName;
            public double amount;
            public double maxAmount;
            public CacheTimeWarpBuffer timeWarpOverflow;
            public List<ProtoPartResourceSnapshot> protoPartResourceSnapshot;

            /// <summary>
            /// Create a CacheResource
            /// </summary>
            /// <param name="inputprotoPartResourceSnapshot"></param>
            /// <param name="resourcename"></param>
            /// <param name="inputamount"></param>
            /// <param name="maxamount"></param>
            public CacheResource(ProtoPartResourceSnapshot inputprotoPartResourceSnapshot, string resourcename, double inputamount, double maxamount)
            {
                protoPartResourceSnapshot = new List<ProtoPartResourceSnapshot>();
                this.protoPartResourceSnapshot.Add(inputprotoPartResourceSnapshot);
                this.resourceName = resourcename;
                this.amount = inputamount;
                this.maxAmount = maxamount;
                this.timeWarpOverflow = new CacheTimeWarpBuffer();
            }
        }
        
        /// <summary>
        /// Get the CacheResource for a particular Resource from the CachedResources Dictionary.
        /// If it does not exist it will return null.
        /// </summary>
        /// <param name="vessel"></param>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        public static CacheResource GetcachedVesselResource(ProtoVessel vessel, string resourceName)
        {
            if (UnloadedResources.InterestedVessels == null)
            {
                return null;
            }
            if (UnloadedResources.InterestedVessels.Contains(vessel))
            {
                List<CacheResource> vslresources = UnloadedResources.InterestedVessels[vessel].CachedResources;
                for (int i = 0; i < vslresources.Count; i++)
                {
                    if (vslresources[i].resourceName == resourceName)
                    {
                        return vslresources[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Create or update a InterestedVessels entry's CachedResources for a ProtoVessel.
        /// </summary>
        /// <param name="vessel"></param>
        public static void CreatecachedVesselResources(ProtoVessel vessel)
        {
            if (UnloadedResources.InterestedVessels == null)
            {
                UnloadedResources.InterestedVessels = new DictionaryValueList<ProtoVessel, InterestedVessel>();
            }
            List<CacheResource> cacheresources = new List<CacheResource>();
            for (int i = 0; i < vessel.protoPartSnapshots.Count; i++)
            {
                ProtoPartSnapshot protoPartSnapshot = vessel.protoPartSnapshots[i];
                for (int j = 0; j < protoPartSnapshot.resources.Count; j++)
                {
                    ProtoPartResourceSnapshot protoPartResourceSnapshot = protoPartSnapshot.resources[j];
                    bool found = false;
                    for (int k = 0; k < cacheresources.Count; k++)
                    {
                        if (cacheresources[k].resourceName == protoPartResourceSnapshot.resourceName)
                        {
                            cacheresources[k].amount += protoPartResourceSnapshot.amount;
                            cacheresources[k].maxAmount += protoPartResourceSnapshot.maxAmount;
                            cacheresources[k].protoPartResourceSnapshot.Add(protoPartResourceSnapshot);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        CacheResource newresource = new CacheResource(protoPartResourceSnapshot, protoPartResourceSnapshot.resourceName, protoPartResourceSnapshot.amount, protoPartResourceSnapshot.maxAmount);
                        cacheresources.Add(newresource);
                    }
                }
            }
            if (UnloadedResources.InterestedVessels.Contains(vessel))
            {
                UnloadedResources.InterestedVessels[vessel].CachedResources = cacheresources;
            }
            else
            {
                InterestedVessel iVessel = new InterestedVessel(vessel.vesselRef, vessel);
                iVessel.CachedResources = cacheresources;
                UnloadedResources.InterestedVessels.Add(vessel, iVessel);
                UnloadedResources.InterestedVessels[vessel].TimeLastRefresh = Time.time;
            }
        }
    }

    /// <summary>
    /// Static Class for processing Unloaded Vessel/Part Resources.
    /// </summary>
    public static class UnloadedResourceProcessing
    {
        /// <summary>
        /// Looks for a CachedResources entry for the passed in ProtoVessel. If one doesn't exist it will create one.
        /// Then it will search the CachedResources for the ProtoVessel for the passed in resourceName and return the amount and maxAmount available
        /// on the ProtoVessel.
        /// </summary>
        /// <param name="vessel"></param>
        /// <param name="resourceName"></param>
        /// <param name="amount"></param>
        /// <param name="maxAmount"></param>
        public static void GetResourceTotals(ProtoVessel vessel, string resourceName, out double amount, out double maxAmount)
        {
            amount = 0d;
            maxAmount = 0d;
            if (UnloadedResources.InterestedVessels == null)
            {
                UnloadedResources.InterestedVessels = new DictionaryValueList<ProtoVessel, InterestedVessel>();
            }
            if (!UnloadedResources.InterestedVessels.Contains(vessel))
            {
                CacheResources.CreatecachedVesselResources(vessel);
            }
            //If there are no cachedResources for the vessel create one.
            if (UnloadedResources.InterestedVessels[vessel].CachedResources == null)
            {
                CacheResources.CreatecachedVesselResources(vessel);
            }
            //If BackgroundProcessing is installed and our cache hasn't been refreshed in 3 mins. Then refresh it.
            if (UnloadedResources.Instance.BackgroundProcessingInstalled &&
                (Time.time - UnloadedResources.InterestedVessels[vessel].TimeLastRefresh > 180))
            {
                CacheResources.CreatecachedVesselResources(vessel);
            }
            //Double check, not really necessary. Now find the resource amounts if in the vessel.
            if (UnloadedResources.InterestedVessels.Contains(vessel))
            {
                List<CacheResources.CacheResource> vslresources = UnloadedResources.InterestedVessels[vessel].CachedResources;
                for (int i = 0; i < vslresources.Count; i++)
                {
                    if (vslresources[i].resourceName == resourceName)
                    {
                        amount = vslresources[i].amount;
                        //amount += vslresources[i].timeWarpOverflow;
                        maxAmount = vslresources[i].maxAmount;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Request Resource processing on a ProtoVessel.
        /// If the ProtoVessel is not known or resources not cached for the ProtoVessel will automatically add them to the Cached data.
        /// </summary>
        /// <param name="vessel">ProtoVessel reference</param>
        /// <param name="resourceName">Name of the Resource we want to process</param>
        /// <param name="amount">The amount of the resource we want to process</param>
        /// <param name="amountReceived">returns the amount processed for the request in this variable</param>
        /// <param name="pushing">default of false (which means take resource). If true will push (put resource)</param>
        public static void RequestResource(ProtoVessel vessel, string resourceName, double amount, out double amountReceived, bool pushing = false)
        {
            amountReceived = 0d;
            if (UnloadedResources.InterestedVessels == null)
            {
                UnloadedResources.InterestedVessels = new DictionaryValueList<ProtoVessel, InterestedVessel>();
            }
            //If there are no cachedResources for the vessel create one.
            if (!UnloadedResources.InterestedVessels.Contains(vessel))
            {
                CacheResources.CreatecachedVesselResources(vessel);
            }

            //Double check, not really necessary. Now find the resource amounts if in the vessel.
            if (UnloadedResources.InterestedVessels.Contains(vessel))
            {
                List<CacheResources.CacheResource> vslresources = UnloadedResources.InterestedVessels[vessel].CachedResources;
                for (int i = 0; i < vslresources.Count; i++)
                {
                    CacheResources.CacheResource cacheResource = vslresources[i];
                    if (cacheResource.resourceName == resourceName)
                    {
                        if (!pushing)  //We are taking resource
                        {
                            if (cacheResource.amount > 0 || cacheResource.timeWarpOverflow.totalAmount > 0)
                            {
                                for (int j = 0; j < cacheResource.protoPartResourceSnapshot.Count; j++)
                                {
                                    ProtoPartResourceSnapshot partResourceSnapshot = cacheResource.protoPartResourceSnapshot[j];
                                    if (cacheResource.timeWarpOverflow.totalAmount > 0 && TimeWarp.fetch != null && TimeWarp.CurrentRateIndex > CacheResources.timeWarpStep) //If we have timewarp Overflow check that first.
                                    {
                                        double amountTaken = 0;
                                        cacheResource.timeWarpOverflow.Take(amount, out amountTaken);
                                        amountReceived += amountTaken;
                                        amount -= amountTaken;
                                        if (amount <= 0) //Did we get all we need already? If so return.
                                        {
                                            return;
                                        }
                                    }
                                    //TimewarpOverflow didn't have enough or didn't have what we need. so now the partResrouceSnapshot
                                    if (partResourceSnapshot.amount > 0)
                                    {
                                        if (partResourceSnapshot.amount <= amount) //Not enough but take what it has
                                        {
                                            amountReceived += partResourceSnapshot.amount;
                                            amount -= partResourceSnapshot.amount;
                                            cacheResource.amount -= partResourceSnapshot.amount;
                                            partResourceSnapshot.amount = 0;
                                        }
                                        else //this part has more than we need.
                                        {
                                            amountReceived += amount;
                                            cacheResource.amount -= amount;
                                            partResourceSnapshot.amount -= amount;
                                            amount = 0;
                                        }
                                    }
                                    if (amount <= 0) //Did we get all we wanted? if so return.
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                        else  //We are putting a resource
                        {
                            //Get how much space there is in this part.
                            double spaceAvailable = cacheResource.maxAmount - cacheResource.amount;
                            if (spaceAvailable > 0) //If we have space put some in.
                            {
                                for (int j = 0; j < cacheResource.protoPartResourceSnapshot.Count; j++)
                                {
                                    ProtoPartResourceSnapshot partResourceSnapshot = cacheResource.protoPartResourceSnapshot[j];
                                    double partspaceAvailable = partResourceSnapshot.maxAmount - partResourceSnapshot.amount;
                                    if (partspaceAvailable > 0)
                                    {
                                        if (amount >= partspaceAvailable
                                        ) //If we can't fit it all in this part. Put what we can.
                                        {
                                            partResourceSnapshot.amount = partResourceSnapshot.maxAmount;
                                            cacheResource.amount += partspaceAvailable;
                                            amount -= partspaceAvailable;
                                            amountReceived += partspaceAvailable;
                                        }
                                        else //If we can fit it all in this part, put it in.
                                        {
                                            partResourceSnapshot.amount += amount;
                                            cacheResource.amount += amount;
                                            amountReceived += amount;
                                            amount = 0;
                                        }
                                        if (amount <= 0) //Did we get all we wanted? if so return.
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                            //If we get here we had more than can fit in the parts... But if TimeWarp is too high, we put it in the overflow.
                            if (TimeWarp.fetch != null && amount > 0)
                            {
                                if (TimeWarp.CurrentRateIndex > CacheResources.timeWarpStep) //But only if timewarp rate is high enough.
                                {
                                    cacheResource.timeWarpOverflow.Add(amount);
                                    amountReceived += amount;
                                    amount = 0;
                                }
                            }
                        }
                    }
                } //End For loop all vessel resources.
            }
        }
    }
    #endregion
}
