using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BackgroundResources
{
    #region UnloadedResourceProcessing
    /// <summary>
    /// Used to Cache Resources for ProtoVessels
    /// </summary>
    public static class CacheResources
    {
        /// <summary>
        /// Information about a Resource and reference to it's ProtoPartResourceSnapshot.
        /// </summary>
        public class CacheResource
        {
            public string resourceName;
            public double amount;
            public double maxAmount;
            public ProtoPartResourceSnapshot protoPartResourceSnapshot;

            /// <summary>
            /// Create a CacheResource
            /// </summary>
            /// <param name="inputprotoPartResourceSnapshot"></param>
            /// <param name="resourcename"></param>
            /// <param name="inputamount"></param>
            /// <param name="maxamount"></param>
            public CacheResource(ProtoPartResourceSnapshot inputprotoPartResourceSnapshot, string resourcename, double inputamount, double maxamount)
            {
                this.protoPartResourceSnapshot = inputprotoPartResourceSnapshot;
                this.resourceName = resourcename;
                this.amount = inputamount;
                this.maxAmount = maxamount;
            }
        }
        /// <summary>
        /// DictionaryValueList of ProtoVessel and Lists of their CacheResources.
        /// </summary>
        public static DictionaryValueList<ProtoVessel, List<CacheResource>> CachedResources { get; private set; }

        /// <summary>
        /// Get the CacheResource for a particular Resource from the CachedResources Dictionary.
        /// If it does not exist it will return null.
        /// </summary>
        /// <param name="vessel"></param>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        public static CacheResource GetcachedVesselResource(ProtoVessel vessel, string resourceName)
        {
            if (CachedResources == null)
            {
                CachedResources = new DictionaryValueList<ProtoVessel, List<CacheResource>>();
                return null;
            }
            if (CachedResources.Contains(vessel))
            {
                List<CacheResource> vslresources = CachedResources[vessel];
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
        /// Create an entry in the CahedResources dictionary for a ProtoVessel if it doesn't exist.
        /// </summary>
        /// <param name="vessel"></param>
        public static void CreatecachedVesselResources(ProtoVessel vessel)
        {
            if (CachedResources == null)
            {
                CachedResources = new DictionaryValueList<ProtoVessel, List<CacheResource>>();
            }
            if (CachedResources.Contains(vessel))
            {
                return;
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
            CachedResources.Add(vessel, cacheresources);
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
            amount = maxAmount = 0d;
            if (CacheResources.CachedResources == null)
            {
                CacheResources.CreatecachedVesselResources(vessel);
            }
            //If there are no cachedResources for the vessel create one.
            if (!CacheResources.CachedResources.Contains(vessel))
            {
                CacheResources.CreatecachedVesselResources(vessel);
            }
            //Double check, not really necessary. Now find the resource amounts if in the vessel.
            if (CacheResources.CachedResources.Contains(vessel))
            {
                List<CacheResources.CacheResource> vslresources = CacheResources.CachedResources[vessel];
                for (int i = 0; i < vslresources.Count; i++)
                {
                    if (vslresources[i].resourceName == resourceName)
                    {
                        amount = vslresources[i].amount;
                        maxAmount = vslresources[i].maxAmount;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vessel"></param>
        /// <param name="resourceName"></param>
        /// <param name="amount"></param>
        /// <param name="amountReceived"></param>
        public static void RequestResource(ProtoVessel vessel, string resourceName, double amount, out double amountReceived, bool pushing = false)
        {
            amountReceived = 0d;
            //If there are no cachedResources for the vessel create one.
            if (!CacheResources.CachedResources.Contains(vessel))
            {
                CacheResources.CreatecachedVesselResources(vessel);
            }

            //Double check, not really necessary. Now find the resource amounts if in the vessel.
            if (CacheResources.CachedResources.Contains(vessel))
            {
                List<CacheResources.CacheResource> vslresources = CacheResources.CachedResources[vessel];
                for (int i = 0; i < vslresources.Count; i++)
                {
                    CacheResources.CacheResource cacheResource = vslresources[i];
                    if (cacheResource.resourceName == resourceName)
                    {
                        if (!pushing)  //We are taking resource
                        {
                            if (cacheResource.amount > 0)
                            {
                                if (cacheResource.amount <= amount)
                                {
                                    amountReceived += cacheResource.amount;
                                    amount -= cacheResource.amount;
                                    cacheResource.amount = 0;
                                    cacheResource.protoPartResourceSnapshot.amount = 0;
                                }
                                else //this part has more than we need.
                                {
                                    amountReceived += amount;
                                    cacheResource.amount -= amount;
                                    cacheResource.protoPartResourceSnapshot.amount -= amount;
                                    amount = 0;
                                }
                                if (amount == 0)  //Did we get all we wanted? if so return.
                                {
                                    return;
                                }
                            }
                        }
                        else  //We are putting a resource
                        {
                            //Get how much space there is in this part.
                            double spaceAvailable = cacheResource.maxAmount - cacheResource.amount;
                            if (spaceAvailable > 0) //If we have space put some in.
                            {
                                if (amount >= spaceAvailable) //If we can't fit it all in this part. Put what we can.
                                {
                                    cacheResource.amount = cacheResource.maxAmount;
                                    cacheResource.protoPartResourceSnapshot.amount = cacheResource.maxAmount;
                                    amount -= spaceAvailable;
                                    amountReceived += spaceAvailable;
                                }
                                else  //If we can fit it all in this part, put it in.
                                {
                                    cacheResource.amount += amount;
                                    cacheResource.protoPartResourceSnapshot.amount += amount;
                                    amountReceived += amount;
                                    amount = 0;
                                }
                                if (amount == 0)  //Did we get all we wanted? if so return.
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion
}
