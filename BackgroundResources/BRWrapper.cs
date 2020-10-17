using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
// TODO: Change this namespace to something specific to your plugin here.
//EG:
namespace MyPlugin_BRWrapper
{
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // BELOW HERE SHOULD NOT BE EDITED - this links to the loaded DeepFreeze module without requiring a Hard Dependancy
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// The Wrapper class to access BackgroundResources from another plugin
    /// </summary>
    public class BRWrapper
    {
        internal static System.Type DFType;
        internal static System.Type KerbalInfoType;
        internal static System.Type VesselInfoType;
        internal static System.Type PartInfoType;
        internal static System.Type DeepFreezerType;
        internal static System.Type FrznCrewMbrType;
        internal static Object actualDF = null;

        /// <summary>
        /// This is the DeepFreeze object
        ///
        /// SET AFTER INIT
        /// </summary>
        public static DFAPI DeepFreezeAPI;

        /// <summary>
        /// Whether we found the DeepFreeze assembly in the loadedassemblies.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return (DFType != null); } }

        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return (DeepFreezeAPI != null); } }

        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance.
        ///
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _DFWrapped;

        /// <summary>
        /// Whether the object has been wrapped and the APIReady flag is set in the real DeepFreeze
        /// </summary>
        public static Boolean APIReady { get { return _DFWrapped && DeepFreezeAPI.APIReady; } }

        /// <summary>
        /// This method will set up the DeepFreeze object and wrap all the methods/functions
        /// </summary>
        /// <returns>
        /// Bool indicating success of call
        /// </returns>
        public static Boolean InitDFWrapper()
        {
            try
            {
                //reset the internal objects
                _DFWrapped = false;
                actualDF = null;
                DeepFreezeAPI = null;
                DFType = null;
                KerbalInfoType = null;
                VesselInfoType = null;
                PartInfoType = null;
                DeepFreezerType = null;
                FrznCrewMbrType = null;
                LogFormatted("Attempting to Grab DeepFreeze Types...");

                //find the base type
                DFType = getType("DF.DeepFreeze");

                if (DFType == null)
                {
                    return false;
                }

                LogFormatted("DeepFreeze Version:{0}", DFType.Assembly.GetName().Version.ToString());

                //now the KerbalInfo Type
                KerbalInfoType = getType("DF.KerbalInfo");

                if (KerbalInfoType == null)
                {
                    return false;
                }

                //now the VesselInfo Type
                VesselInfoType = getType("DF.VesselInfo");

                if (VesselInfoType == null)
                {
                    return false;
                }

                //now the PartInfo Type
                PartInfoType = getType("DF.PartInfo");

                if (PartInfoType == null)
                {
                    return false;
                }

                //now the DeepFreezer (partmodule) Type
                DeepFreezerType = getType("DF.DeepFreezer");

                if (DeepFreezerType == null)
                {
                    return false;
                }

                //now the FrznCrewMbr Type
                FrznCrewMbrType = getType("DF.FrznCrewMbr");
                if (FrznCrewMbrType == null)
                {
                    return false;
                }

                //now grab the running instance
                LogFormatted("Got Assembly Types, grabbing Instance");
                try
                {
                    actualDF = DFType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                }
                catch (Exception)
                {
                    LogFormatted("No Instance found - most likely you have an old DeepFreeze installed");
                    return false;
                }
                if (actualDF == null)
                {
                    LogFormatted("Failed grabbing Instance");
                    return false;
                }

                //If we get this far we can set up the local object and its methods/functions
                LogFormatted("Got Instance, Creating Wrapper Objects");
                DeepFreezeAPI = new DFAPI(actualDF);
                _DFWrapped = true;
                return true;
            }
            catch (Exception ex)
            {
                LogFormatted("Unable to setup InitDFWrapper Reflection");
                LogFormatted("Exception: {0}", ex);
                _DFWrapped = false;
                return false;
            }
        }

        internal static Type getType(string name)
        {
            Type type = null;
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>

            {
                if (t.FullName == name)
                    type = t;
            }
            );

            if (type != null)
            {
                return type;
            }
            return null;
        }

        /// <summary>
        /// The API Class that is an analogue of the real DeepFreeze. This lets you access all the API-able properties and Methods of the DeepFreeze
        /// </summary>
        public class DFAPI
        {
            internal DFAPI(Object a)
            {
                try
                {
                    //store the actual object
                    actualDFAPI = a;

                    //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                    //for events we also add a handler
                    //Object tstfrozenkerbals = DFType.GetField("FrozenKerbals", BindingFlags.Public | BindingFlags.Static).GetValue(null);

                    LogFormatted("Getting APIReady Object");
                    APIReadyField = DFType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static);
                    LogFormatted("Success: " + (APIReadyField != null).ToString());

                    LogFormatted("Getting KillFrozenCrew Method Object");
                    ActualKillFrozenCrewMethod = DFType.GetMethod("KillFrozenCrew", BindingFlags.Public | BindingFlags.Instance);
                    LogFormatted("Success: " + (ActualKillFrozenCrewMethod != null).ToString());

                    LogFormatted("Getting FrozenKerbals Object");
                    FrozenKerbalsMethod = DFType.GetMethod("get_FrozenKerbals", BindingFlags.Public | BindingFlags.Instance);
                    actualFrozenKerbals = FrozenKerbalsMethod.Invoke(actualDFAPI, null);
                    LogFormatted("Success: " + (actualFrozenKerbals != null).ToString());

                    LogFormatted("Getting FrozenKerbals Object");
                    FrozenKerbalsListMethod = DFType.GetMethod("get_FrozenKerbalsList", BindingFlags.Public | BindingFlags.Instance);
                    actualFrozenKerbalsList = FrozenKerbalsListMethod.Invoke(actualDFAPI, null);
                    LogFormatted("Success: " + (actualFrozenKerbalsList != null).ToString());

                    LogFormatted("Getting KnownVessels Object");
                    knownVesselsMethod = DFType.GetMethod("get_KnownVessels", BindingFlags.Public | BindingFlags.Instance);
                    actualKnownVessels = knownVesselsMethod.Invoke(actualDFAPI, null);
                    LogFormatted("Success: " + (actualKnownVessels != null).ToString());

                    LogFormatted("Getting KnownVessels Object");
                    knownVesselsListMethod = DFType.GetMethod("get_KnownVesselsList", BindingFlags.Public | BindingFlags.Instance);
                    actualKnownVesselsList = knownVesselsListMethod.Invoke(actualDFAPI, null);
                    LogFormatted("Success: " + (actualKnownVesselsList != null).ToString());

                    LogFormatted("Getting KnownFreezerParts Object");
                    knownFreezerPartsMethod = DFType.GetMethod("get_KnownFreezerParts", BindingFlags.Public | BindingFlags.Instance);
                    actualKnownFreezerParts = knownFreezerPartsMethod.Invoke(actualDFAPI, null);
                    LogFormatted("Success: " + (actualKnownFreezerParts != null).ToString());

                    LogFormatted("Getting KnownFreezerParts Object");
                    knownFreezerPartsListMethod = DFType.GetMethod("get_KnownFreezerPartsList", BindingFlags.Public | BindingFlags.Instance);
                    actualKnownFreezerPartsList = knownFreezerPartsListMethod.Invoke(actualDFAPI, null);
                    LogFormatted("Success: " + (actualKnownFreezerPartsList != null).ToString());

                    LogFormatted("Getting EcRequired Object");
                    ActualECReqdMethod = DFType.GetMethod("get_DFIECReqd");
                    actualECReqdField = ActualECReqdMethod.Invoke(actualDFAPI, null);
                    LogFormatted("Success: " + (actualECReqdField != null).ToString());

                    LogFormatted("Getting DeathFatal Object");
                    ActualDeathFatalMethod = DFType.GetMethod("get_DFIDeathFatal");
                    ActualDeathFatalField = ActualDeathFatalMethod.Invoke(actualDFAPI, null);
                    LogFormatted("Success: " + (ActualDeathFatalField != null).ToString());
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to Instantiate DFAPI object using Reflection");
                    LogFormatted("Exception: {0}", ex);
                }
            }

            private Object actualDFAPI;

            private FieldInfo APIReadyField;
            /// <summary>
            /// Whether the APIReady flag is set in the real KAC
            /// </summary>
            /// <returns>
            /// Bool Indicating if DeepFreeze is ready for API calls
            /// </returns>
            public bool APIReady
            {
                get
                {
                    if (APIReadyField == null)
                        return false;

                    return (Boolean)APIReadyField.GetValue(null);
                }
            }

            private object actualECReqdField;
            private MethodInfo ActualECReqdMethod;
            /// <summary>
            /// Is EC required to run the DeepFreeze Freezers (Deepfreeze difficulty setting)
            /// </summary>
            public bool ECReqd
            {
                get
                {
                    if (actualECReqdField == null)
                        return false;

                    return (Boolean)actualECReqdField;
                }
            }

            private object ActualDeathFatalField;
            private MethodInfo ActualDeathFatalMethod;
            /// <summary>
            /// Is Death fatal or do kerals respawn? (Deepfreeze difficulty setting)
            /// </summary>
            public bool DeathFatal
            {
                get
                {
                    if (ActualDeathFatalField == null)
                        return false;

                    return (Boolean)ActualDeathFatalField;
                }
            }

            private MethodInfo ActualKillFrozenCrewMethod;

            public void KillFrozenCrew(string crewMember)
            {
                ActualKillFrozenCrewMethod.Invoke(actualDFAPI, new object[] { crewMember });
            }

            #region Frozenkerbals

            private object actualFrozenKerbals;
            private MethodInfo FrozenKerbalsMethod;

            /// <summary>
            /// The dictionary of Frozen Kerbals that are currently active in game
            /// </summary>
            /// <returns>
            /// Dictionary <string, KerbalInfo> of Frozen Kerbals
            /// </returns>
            internal Dictionary<string, KerbalInfo> FrozenKerbals
            {
                get
                {
                    Dictionary<string, KerbalInfo> returnvalue = new Dictionary<string, KerbalInfo>();
                    if (FrozenKerbalsMethod == null)
                    {
                        LogFormatted("Error getting FrozenKerbals - Reflection Method is Null");
                        return returnvalue;
                    }
                    //object actualDFtest = DFType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    //FieldInfo gamesettingsfield = DFType.GetField("DFgameSettings", BindingFlags.Instance | BindingFlags.NonPublic);
                    //object gamesettings;
                    //if (gamesettingsfield != null)
                    //    gamesettings = gamesettingsfield.GetValue(actualDFAPI);
                    actualFrozenKerbals = null;
                    actualFrozenKerbals = FrozenKerbalsMethod.Invoke(actualDFAPI, null);
                    returnvalue = ExtractFrozenKerbalDict(actualFrozenKerbals);
                    return returnvalue;
                }
            }

            /// <summary>
            /// This converts the actualFrozenKerbals actual object to a new dictionary for consumption
            /// </summary>
            /// <param name="actualFrozenKerbals"></param>
            /// <returns>
            /// Dictionary <string, KerbalInfo> of Frozen Kerbals
            /// </returns>
            private Dictionary<string, KerbalInfo> ExtractFrozenKerbalDict(Object actualFrozenKerbals)
            {
                Dictionary<string, KerbalInfo> DictToReturn = new Dictionary<string, KerbalInfo>();
                try
                {
                    foreach (var item in (IDictionary)actualFrozenKerbals)
                    {
                        var typeitem = item.GetType();
                        PropertyInfo[] itemprops = typeitem.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        string itemkey = (string)itemprops[0].GetValue(item, null);
                        object itemvalue = (object)itemprops[1].GetValue(item, null);
                        KerbalInfo itemkerbalinfo = new KerbalInfo(itemvalue);
                        DictToReturn[itemkey] = itemkerbalinfo;
                    }
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to extract FrozenKerbals Dictionary: {0}", ex.Message);
                }
                return DictToReturn;
            }

            private object actualFrozenKerbalsList;
            private MethodInfo FrozenKerbalsListMethod;

            /// <summary>
            /// This converts the actualFrozenKerbals actual object to a new List keyvaluepair for consumption
            /// </summary>
            /// /// <returns>
            /// List<KeyValuePair<string, KerbalInfo>> of Frozen Kerbals
            /// </returns>
            internal List<KeyValuePair<string, KerbalInfo>> FrozenKerbalsList
            {
                get
                {
                    List<KeyValuePair<string, KerbalInfo>> returnvalue = new List<KeyValuePair<string, KerbalInfo>>();
                    if (FrozenKerbalsListMethod == null)
                    {
                        LogFormatted("Error getting FrozenKerbals - Reflection Method is Null");
                        return returnvalue;
                    }
                    actualFrozenKerbalsList = null;
                    actualFrozenKerbalsList = FrozenKerbalsListMethod.Invoke(actualDFAPI, null);
                    returnvalue = (List<KeyValuePair<string, KerbalInfo>>)actualFrozenKerbalsList;
                    return returnvalue;
                }
            }

            #endregion Frozenkerbals

            #region KnownVessels

            private object actualKnownVessels;
            private MethodInfo knownVesselsMethod;

            /// <summary>
            /// The dictionary of Known Vessels that are currently active in game
            /// </summary>
            /// <returns>
            /// Dictionary <Guid, VesselInfo> of Known Vessels
            /// </returns>
            internal Dictionary<Guid, VesselInfo> KnownVessels
            {
                get
                {
                    Dictionary<Guid, VesselInfo> returnvalue = new Dictionary<Guid, VesselInfo>();
                    if (knownVesselsMethod == null)
                    {
                        LogFormatted("Error getting KnownVessels - Reflection Method is Null");
                        return returnvalue;
                    }
                    actualKnownVessels = null;
                    actualKnownVessels = knownVesselsMethod.Invoke(actualDFAPI, null);
                    returnvalue = ExtractKnownVesselsDict(actualKnownVessels);
                    return returnvalue;
                }
            }

            /// <summary>
            /// This converts the actualKnownVessels actual object to a new dictionary for consumption
            /// </summary>
            /// <param name="actualKnownVessels"></param>
            /// <returns>
            /// Dictionary <string, VesselInfo> of Known Vessels
            /// </returns>
            private Dictionary<Guid, VesselInfo> ExtractKnownVesselsDict(Object actualKnownVessels)
            {
                Dictionary<Guid, VesselInfo> DictToReturn = new Dictionary<Guid, VesselInfo>();
                try
                {
                    foreach (var item in (IDictionary)actualKnownVessels)
                    {
                        var typeitem = item.GetType();
                        PropertyInfo[] itemprops = typeitem.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        Guid itemkey = (Guid)itemprops[0].GetValue(item, null);
                        object itemvalue = (object)itemprops[1].GetValue(item, null);
                        VesselInfo itemVesselInfo = new VesselInfo(itemvalue);
                        DictToReturn[itemkey] = itemVesselInfo;
                    }
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to extract KnownVessels Dictionary: {0}", ex.Message);
                }
                return DictToReturn;
            }

            private object actualKnownVesselsList;
            private MethodInfo knownVesselsListMethod;

            /// <summary>
            /// This converts the actualKnownVessels actual object to a new List keyvaluepair for consumption
            /// </summary>
            /// /// <returns>
            /// List<KeyValuePair<Guid, VesselInfo>> of Known Vessels
            /// </returns>
            internal List<KeyValuePair<Guid, VesselInfo>> KnownVesselsList
            {
                get
                {
                    List<KeyValuePair<Guid, VesselInfo>> returnvalue = new List<KeyValuePair<Guid, VesselInfo>>();
                    if (knownVesselsListMethod == null)
                    {
                        LogFormatted("Error getting KnownVessels - Reflection Method is Null");
                        return returnvalue;
                    }
                    actualKnownVesselsList = null;
                    actualKnownVesselsList = knownVesselsListMethod.Invoke(actualDFAPI, null);
                    returnvalue = (List<KeyValuePair<Guid, VesselInfo>>)actualKnownVesselsList;
                    return returnvalue;
                }
            }

            #endregion KnownVessels

            #region KnownFreezerParts

            private object actualKnownFreezerParts;
            private MethodInfo knownFreezerPartsMethod;

            /// <summary>
            /// The dictionary of Known Parts that are currently active in game
            /// </summary>
            /// <returns>
            /// Dictionary <uint, PartInfo> of Known Freezer Parts
            /// </returns>
            internal Dictionary<uint, PartInfo> KnownFreezerParts
            {
                get
                {
                    Dictionary<uint, PartInfo> returnvalue = new Dictionary<uint, PartInfo>();
                    if (knownFreezerPartsMethod == null)
                    {
                        LogFormatted("Error getting KnownFreezerParts - Reflection Method is Null");
                        return returnvalue;
                    }
                    actualKnownFreezerParts = null;
                    actualKnownFreezerParts = knownFreezerPartsMethod.Invoke(actualDFAPI, null);
                    returnvalue = ExtractKnownFreezerPartsDict(actualKnownFreezerParts);
                    return returnvalue;
                }
            }

            /// <summary>
            /// This converts the actualKnownFreezerParts actual object to a new dictionary for consumption
            /// </summary>
            /// <param name="actualKnownFreezerParts"></param>
            /// <returns>
            /// Dictionary <uint, PartInfo> of Known Vessels
            /// </returns>
            private Dictionary<uint, PartInfo> ExtractKnownFreezerPartsDict(Object actualKnownFreezerParts)
            {
                Dictionary<uint, PartInfo> DictToReturn = new Dictionary<uint, PartInfo>();
                try
                {
                    foreach (var item in (IDictionary)actualKnownFreezerParts)
                    {
                        var typeitem = item.GetType();
                        PropertyInfo[] itemprops = typeitem.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        uint itemkey = (uint)itemprops[0].GetValue(item, null);
                        object itemvalue = (object)itemprops[1].GetValue(item, null);
                        PartInfo itemPartInfo = new PartInfo(itemvalue);
                        DictToReturn[itemkey] = itemPartInfo;
                    }
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to extract KnownFreezerParts Dictionary: {0}", ex.Message);
                }
                return DictToReturn;
            }

            private object actualKnownFreezerPartsList;
            private MethodInfo knownFreezerPartsListMethod;

            /// <summary>
            /// This converts the actualKnownFreezerParts actual object to a new List keyvaluepair for consumption
            /// </summary>
            /// /// <returns>
            /// List<KeyValuePair<uint, PartInfo>> of Known Freezer Parts
            /// </returns>
            internal List<KeyValuePair<uint, PartInfo>> KnownFreezerPartsList
            {
                get
                {
                    List<KeyValuePair<uint, PartInfo>> returnvalue = new List<KeyValuePair<uint, PartInfo>>();
                    if (knownFreezerPartsListMethod == null)
                    {
                        LogFormatted("Error getting KnownFreezerParts - Reflection Method is Null");
                        return returnvalue;
                    }
                    actualKnownFreezerPartsList = null;
                    actualKnownFreezerPartsList = knownFreezerPartsListMethod.Invoke(actualDFAPI, null);
                    returnvalue = (List<KeyValuePair<uint, PartInfo>>)actualKnownFreezerPartsList;
                    return returnvalue;
                }
            }

            #endregion KnownFreezerParts
        }

        #region DeepFreezerPart

        /// <summary>
        /// The Class that is an analogue of the real DeepFreezer PartModule. This lets you access all the API-able properties and Methods of the DeepFreezer
        /// </summary>
        public class DeepFreezer
        {
            internal DeepFreezer(Object a)
            {
                actualDeepFreezer = a;
                //Fields available from Freezer part
                crewXferTOActiveMethod = DeepFreezerType.GetMethod("get_DFIcrewXferTOActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                crewXferTOActive = getcrewXferTOActive;
                crewXferFROMActiveMethod = DeepFreezerType.GetMethod("get_DFIcrewXferFROMActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                crewXferFROMActive = getcrewXferFROMActive;
                FreezerSizeMethod = DeepFreezerType.GetMethod("get_DFIFreezerSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                FreezerSize = getFreezerSize;
                TotalFrozenMethod = DeepFreezerType.GetMethod("get_DFITotalFrozen", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                TotalFrozen = getTotalFrozen;
                FreezerSpaceMethod = DeepFreezerType.GetMethod("get_DFIFreezerSpace", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                FreezerSpace = getFreezerSpace;
                PartFullMethod = DeepFreezerType.GetMethod("get_DFIPartFull", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                PartFull = getPartFull;
                IsFreezeActiveMethod = DeepFreezerType.GetMethod("get_DFIIsFreezeActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                IsFreezeActive = getIsFreezeActive;
                IsThawActiveMethod = DeepFreezerType.GetMethod("get_DFIIsThawActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                IsThawActive = getIsThawActive;
                FreezerOutofECMethod = DeepFreezerType.GetMethod("get_DFIFreezerOutofEC", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                FreezerOutofEC = getFreezerOutofEC;
                FrzrTmpMethod = DeepFreezerType.GetMethod("get_DFIFrzrTmp", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                FrzrTmp = getFrzrTmp;
                FrznChargeUsageMethod = DeepFreezerType.GetMethod("get_DFIFrznChargeUsage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                FrznChargeUsage = getFrznChargeUsage;
                StoredCrewListMethod = DeepFreezerType.GetMethod("get_DFIStoredCrewList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                actualStoredCrewList = StoredCrewListMethod.Invoke(actualDeepFreezer, null);
                ECReqdMethod = DeepFreezerType.GetMethod("get_DFIECReqd", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                ECReqd = getECReqd;
                FrznChargeRequiredMethod = DeepFreezerType.GetMethod("get_DFIFrznChargeRequired", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                FrznChargeRequired = getFrznChargeRequired;

                //Methods
                //LogFormatted("Getting beginFreezeKerbalMethod Method");
                beginFreezeKerbalMethod = DeepFreezerType.GetMethod("beginFreezeKerbal", BindingFlags.Public | BindingFlags.Instance);
                //LogFormatted_DebugOnly("Success: " + (beginFreezeKerbalMethod != null).ToString());

                //LogFormatted("Getting beginThawKerbalMethod Method");
                beginThawKerbalMethod = DeepFreezerType.GetMethod("beginThawKerbal", BindingFlags.Public | BindingFlags.Instance);
                //LogFormatted_DebugOnly("Success: " + (beginThawKerbalMethod != null).ToString());
            }

            private Object actualDeepFreezer;

            #region DeepFreezerFieldMethods

            /// <summary>
            /// True if a crewXfter TO this DeepFreezer part is currently active
            /// </summary>
            public bool crewXferTOActive;

            private MethodInfo crewXferTOActiveMethod;

            private bool getcrewXferTOActive
            {
                get { return (bool)crewXferTOActiveMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// True if a crewXfter FROM this DeepFreezer part is currently active
            /// </summary>
            public bool crewXferFROMActive;

            private MethodInfo crewXferFROMActiveMethod;

            private bool getcrewXferFROMActive
            {
                get { return (bool)crewXferFROMActiveMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// The number of cryopods in this DeepFreezer
            /// </summary>
            public int FreezerSize;

            private MethodInfo FreezerSizeMethod;

            private int getFreezerSize
            {
                get { return (int)FreezerSizeMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// The number of currently frozen Kerbals in this DeepFreezer
            /// </summary>
            public int TotalFrozen;

            private MethodInfo TotalFrozenMethod;

            private int getTotalFrozen
            {
                get { return (int)TotalFrozenMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// The number of empty cryopods in this DeepFreezer
            /// </summary>
            public int FreezerSpace;

            private MethodInfo FreezerSpaceMethod;

            private int getFreezerSpace
            {
                get { return (int)FreezerSpaceMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// True if all the cryopods are taken in this DeepFreezer (includes, frozen and thawed kerbals).
            /// </summary>
            public bool PartFull;

            private MethodInfo PartFullMethod;

            private bool getPartFull
            {
                get { return (bool)PartFullMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// True if a Freeze kerbal event is currently active in this DeepFreezer
            /// </summary>
            public bool IsFreezeActive;

            private MethodInfo IsFreezeActiveMethod;

            private bool getIsFreezeActive
            {
                get { return (bool)IsFreezeActiveMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// True if a Thaw kerbal event is currently active in this DeepFreezer
            /// </summary>
            public bool IsThawActive;

            private MethodInfo IsThawActiveMethod;

            private bool getIsThawActive
            {
                get { return (bool)IsThawActiveMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// True if this DeepFreezer is currently out of Electric Charge
            /// </summary>
            public bool FreezerOutofEC;

            private MethodInfo FreezerOutofECMethod;

            private bool getFreezerOutofEC
            {
                get { return (bool)FreezerOutofECMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// The current freezer temperature status of this DeepFreezer
            /// </summary>
            public FrzrTmpStatus FrzrTmp;

            private MethodInfo FrzrTmpMethod;

            private FrzrTmpStatus getFrzrTmp
            {
                get { return (FrzrTmpStatus)FrzrTmpMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// True if this DeepFreezer requires Electric Charge for frozen Kerbals
            /// </summary>
            public bool ECReqd;

            private MethodInfo ECReqdMethod;

            private bool getECReqd
            {
                get { return (bool)ECReqdMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// The current EC Usage of this DeepFreezer
            /// </summary>
            public float FrznChargeUsage;

            private MethodInfo FrznChargeUsageMethod;

            private float getFrznChargeUsage
            {
                get { return (float)FrznChargeUsageMethod.Invoke(actualDeepFreezer, null); }
            }

            /// <summary>
            /// The EC Usage of this DeepFreezer Per Kerbal per minute
            /// </summary>
            public int FrznChargeRequired;

            private MethodInfo FrznChargeRequiredMethod;

            private int getFrznChargeRequired
            {
                get { return (int)FrznChargeRequiredMethod.Invoke(actualDeepFreezer, null); }
            }

            private Object actualStoredCrewList;
            private MethodInfo StoredCrewListMethod;

            /// <summary>
            /// a List<FrznCrewMbr> of all Frozen Crew in this DeepFreezer
            /// </summary>
            public FrznCrewList StoredCrewList
            {
                get { return ExtractStoredCrewList(actualStoredCrewList); }
            }

            /// <summary>
            /// This converts the StoredCrewList actual object to a new List for consumption
            /// </summary>
            /// <param name="actualStoredCrewList"></param>
            /// <returns></returns>
            private FrznCrewList ExtractStoredCrewList(Object actualStoredCrewList)
            {
                FrznCrewList ListToReturn = new FrznCrewList();
                try
                {
                    //iterate each "value" in the dictionary

                    foreach (var item in (IList)actualStoredCrewList)
                    {
                        FrznCrewMbr r1 = new FrznCrewMbr(item);
                        ListToReturn.Add(r1);
                    }
                }
                catch (Exception ex)
                {
                    LogFormatted("Arrggg: {0}", ex.Message);
                    //throw ex;
                    //
                }
                return ListToReturn;
            }

            #endregion DeepFreezerFieldMethods

            #region DeepFreezerMethods

            private MethodInfo beginFreezeKerbalMethod;

            /// <summary>
            /// Begin the Freezing of a Kerbal
            /// </summary>
            /// <param name="CrewMember">ProtoCrewMember that you want frozen</param>
            /// <returns>Bool indicating success of call</returns>
            public bool beginFreezeKerbal(ProtoCrewMember CrewMember)
            {
                try
                {
                    beginFreezeKerbalMethod.Invoke(actualDeepFreezer, new System.Object[] { CrewMember });
                    return true;
                }
                catch (Exception ex)
                {
                    LogFormatted("Arrggg: {0}", ex.Message);
                    return false;
                }
            }

            private MethodInfo beginThawKerbalMethod;

            /// <summary>
            /// Begin the Thawing of a Kerbal
            /// </summary>
            /// <param name="frozenkerbal">string containing the name of the kerbal you want thawed</param>
            /// <returns>Bool indicating success of call</returns>
            public bool beginThawKerbal(string frozenkerbal)
            {
                try
                {
                    beginThawKerbalMethod.Invoke(actualDeepFreezer, new System.Object[] { frozenkerbal });
                    return true;
                }
                catch (Exception ex)
                {
                    LogFormatted("Arrggg: {0}", ex.Message);
                    return false;
                }
            }

            #endregion DeepFreezerMethods
        }

        public enum FrzrTmpStatus
        {
            OK = 0,
            WARN = 1,
            RED = 2,
        }

        /// <summary>
        /// The Class that is an analogue of the real FrznCrewMbr as part of the StoredCrewList field in the DeepFreezer PartModule.
        /// </summary>
        public class FrznCrewMbr
        {
            internal FrznCrewMbr(Object a)
            {
                actualFrznCrewMbr = a;
                CrewNameMethod = FrznCrewMbrType.GetMethod("get_CrewName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                CrewName = getCrewName;
                SeatIdxMethod = FrznCrewMbrType.GetMethod("get_SeatIdx", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                SeatIdx = getSeatIdx;
                VesselIDMethod = FrznCrewMbrType.GetMethod("get_VesselID", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                VesselID = getVesselID;
                VesselNameMethod = FrznCrewMbrType.GetMethod("get_VesselName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                VesselName = getVesselName;
            }

            private Object actualFrznCrewMbr;


            /// <summary>
            /// Crew Members Name
            /// </summary>
            public string CrewName;

            private MethodInfo CrewNameMethod;

            private string getCrewName
            {
                get { return (string)CrewNameMethod.Invoke(actualFrznCrewMbr, null); }
            }

            /// <summary>
            /// Seat Index for Crew member
            /// </summary>
            public int SeatIdx;

            private MethodInfo SeatIdxMethod;

            private int getSeatIdx
            {
                get { return (int)SeatIdxMethod.Invoke(actualFrznCrewMbr, null); }
            }

            /// <summary>
            /// Vessel ID
            /// </summary>
            public Guid VesselID;

            private MethodInfo VesselIDMethod;

            private Guid getVesselID
            {
                get { return (Guid)VesselIDMethod.Invoke(actualFrznCrewMbr, null); }
            }

            /// <summary>
            /// Vessel Name
            /// </summary>
            public string VesselName;

            private MethodInfo VesselNameMethod;

            private string getVesselName
            {
                get { return (string)VesselNameMethod.Invoke(actualFrznCrewMbr, null); }
            }
        }

        public class FrznCrewList : List<FrznCrewMbr>
        {
        }

        #endregion DeepFreezerPart

        /// <summary>
        /// The Value Class of the FrozenCrewList Dictionary that is an analogue of the real FrozenKerbals Dictionary in the DeepFreezer Class.
        /// </summary>
        public class KerbalInfo
        {
            internal KerbalInfo(Object a)
            {
                actualFrozenKerbalInfo = a;
                lastUpdateField = KerbalInfoType.GetField("lastUpdate");
                statusField = KerbalInfoType.GetField("status");
                typeField = KerbalInfoType.GetField("type");
                vesselIDField = KerbalInfoType.GetField("vesselID");
                vesselNameField = KerbalInfoType.GetField("vesselName");
                partIDField = KerbalInfoType.GetField("partID");
                seatIdxField = KerbalInfoType.GetField("seatIdx");
                seatNameField = KerbalInfoType.GetField("seatName");
                experienceTraitNameField = KerbalInfoType.GetField("experienceTraitName");
            }

            private Object actualFrozenKerbalInfo;

            private FieldInfo lastUpdateField;

            /// <summary>
            /// last time the FrozenKerbalInfo was updated
            /// </summary>
            public double lastUpdate
            {
                get { return (double)lastUpdateField.GetValue(actualFrozenKerbalInfo); }
            }

            private FieldInfo statusField;

            /// <summary>
            /// RosterStatus of the frozen kerbal
            /// </summary>
            public ProtoCrewMember.RosterStatus status
            {
                get { return (ProtoCrewMember.RosterStatus)statusField.GetValue(actualFrozenKerbalInfo); }
            }

            private FieldInfo typeField;

            /// <summary>
            /// KerbalType of the frozen kerbal
            /// </summary>
            public ProtoCrewMember.KerbalType type
            {
                get { return (ProtoCrewMember.KerbalType)typeField.GetValue(actualFrozenKerbalInfo); }
            }

            private FieldInfo vesselIDField;

            /// <summary>
            /// Guid of the vessel the frozen kerbal is aboard
            /// </summary>
            public Guid vesselID
            {
                get { return (Guid)vesselIDField.GetValue(actualFrozenKerbalInfo); }
            }

            private FieldInfo vesselNameField;

            /// <summary>
            /// Name of the vessel the frozen kerbal is aboard
            /// </summary>
            public string vesselName
            {
                get { return (string)vesselNameField.GetValue(actualFrozenKerbalInfo); }
            }

            private FieldInfo partIDField;

            /// <summary>
            /// partID of the vessel part the frozen kerbal is aboard
            /// </summary>
            public uint partID
            {
                get { return (uint)partIDField.GetValue(actualFrozenKerbalInfo); }
            }

            private FieldInfo seatIdxField;

            /// <summary>
            /// seat index that the frozen kerbal is in
            /// </summary>
            public int seatIdx
            {
                get { return (int)seatIdxField.GetValue(actualFrozenKerbalInfo); }
            }

            private FieldInfo seatNameField;

            /// <summary>
            /// seat name that the frozen kerbal is in
            /// </summary>
            public string seatName
            {
                get { return (string)seatNameField.GetValue(actualFrozenKerbalInfo); }
            }

            private FieldInfo experienceTraitNameField;

            /// <summary>
            /// name of the experience trait for the frozen kerbal
            /// </summary>
            public string experienceTraitName
            {
                get { return (string)experienceTraitNameField.GetValue(actualFrozenKerbalInfo); }
            }
        }

        /// <summary>
        /// The Value Class of the KnownVesselsList Dictionary that is an analogue of the real KnownVessels Dictionary in the DeepFreezer Class.
        /// </summary>
        public class VesselInfo
        {
            internal VesselInfo(Object a)
            {
                actualVesselInfo = a;
                vesselNameField = VesselInfoType.GetField("vesselName");
                vesselTypeField = VesselInfoType.GetField("vesselType");
                numCrewField = VesselInfoType.GetField("numCrew");
                numSeatsField = VesselInfoType.GetField("numSeats");
                numOccupiedPartsField = VesselInfoType.GetField("numOccupiedParts");
                numFrozenCrewField = VesselInfoType.GetField("numFrznCrew");
                hibernatingField = VesselInfoType.GetField("hibernating");
                hasExternalDoorField = VesselInfoType.GetField("hasextDoor");
                hasExternalPodField = VesselInfoType.GetField("hasextPod");
                lastUpdateField = VesselInfoType.GetField("lastUpdate");
                storedECField = VesselInfoType.GetField("storedEC");
                predictedECOutField = VesselInfoType.GetField("predictedECOut");
            }

            private Object actualVesselInfo;

            private FieldInfo vesselNameField;

            /// <summary>
            /// The Vessel Name
            /// </summary>
            public string vesselName
            {
                get { return (string)vesselNameField.GetValue(actualVesselInfo); }
            }
            private FieldInfo vesselTypeField;

            /// <summary>
            /// last time the FrozenKerbalInfo was updated
            /// </summary>
            public VesselType vesselType
            {
                get { return (VesselType)vesselTypeField.GetValue(actualVesselInfo); }
            }

            private FieldInfo numCrewField;

            /// <summary>
            /// Number of Crew
            /// </summary>
            public int numCrew
            {
                get { return (int)numCrewField.GetValue(actualVesselInfo); }
            }

            private FieldInfo numSeatsField;

            /// <summary>
            /// Number of Seats
            /// </summary>
            public int numSeats
            {
                get { return (int)numSeatsField.GetValue(actualVesselInfo); }
            }

            private FieldInfo numOccupiedPartsField;

            /// <summary>
            /// Number of Occupied parts
            /// </summary>
            public int numOccupiedParts
            {
                get { return (int)numOccupiedPartsField.GetValue(actualVesselInfo); }
            }

            private FieldInfo numFrozenCrewField;

            /// <summary>
            /// Number of Frozen Crew
            /// </summary>
            public int numFrozenCrew
            {
                get { return (int)numFrozenCrewField.GetValue(actualVesselInfo); }
            }

            private FieldInfo hibernatingField;

            /// <summary>
            /// Is the vessel hibernating
            /// </summary>
            public bool hibernating
            {
                get { return (bool)hibernatingField.GetValue(actualVesselInfo); }
            }

            private FieldInfo hasExternalDoorField;

            /// <summary>
            /// If there's an external door
            /// </summary>
            public bool hasExternalDoor
            {
                get { return (bool)hasExternalDoorField.GetValue(actualVesselInfo); }
            }

            private FieldInfo hasExternalPodField;

            /// <summary>
            /// If there's an external pod
            /// </summary>
            public bool hasExternalPod
            {
                get { return (bool)hasExternalPodField.GetValue(actualVesselInfo); }
            }

            private FieldInfo lastUpdateField;

            /// <summary>
            /// Last time updated
            /// </summary>
            public double lastUpdate
            {
                get { return (double)lastUpdateField.GetValue(actualVesselInfo); }
            }

            private FieldInfo storedECField;

            /// <summary>
            /// How much EC it has stored
            /// </summary>
            public double storedEC
            {
                get { return (double)storedECField.GetValue(actualVesselInfo); }
                set { storedECField.SetValue(actualVesselInfo, value); }
            }

            private FieldInfo predictedECOutField;

            /// <summary>
            /// Predicted time EC will run out
            /// </summary>
            public double predictedECOut
            {
                get { return (double)predictedECOutField.GetValue(actualVesselInfo); }
            }
        }


        /// <summary>
        /// The Value Class of the KnownFreezerPartsList Dictionary that is an analogue of the real KnownFreezerParts Dictionary in the DeepFreezer Class.
        /// </summary>
        public class PartInfo
        {
            internal PartInfo(Object a)
            {
                actualPartInfo = a;
                vesselIDField = PartInfoType.GetField("vesselID");
                partNameField = PartInfoType.GetField("PartName");
                numSeatsField = PartInfoType.GetField("numSeats");
                numCrewField = PartInfoType.GetField("numCrew");
                crewMembersField = PartInfoType.GetField("crewMembers");
                crewMemberTraitsField = PartInfoType.GetField("crewMemberTraits");
                numFrznCrewField = PartInfoType.GetField("numFrznCrew");
                hibernatingField = PartInfoType.GetField("hibernating");
                hasExternalDoorField = PartInfoType.GetField("hasextDoor");
                hasExternalPodField = PartInfoType.GetField("hasextPod");
                timeLastElectricityField = PartInfoType.GetField("timeLastElectricity");
                frznChargeRequiredField = PartInfoType.GetField("frznChargeRequired");
                timeLastTempCheckField = PartInfoType.GetField("timeLastTempCheck");
                deathCounterField = PartInfoType.GetField("deathCounter");
                tmpdeathCounterField = PartInfoType.GetField("tmpdeathCounter");
                outofECField = PartInfoType.GetField("outofEC");
                TmpStatusField = PartInfoType.GetField("TmpStatus");
                ECWarningField = PartInfoType.GetField("ECWarning");
                TempWarningField = PartInfoType.GetField("TempWarning");
                cabinTempField = PartInfoType.GetField("cabinTemp");
                lastUpdateField = PartInfoType.GetField("lastUpdate");
            }

            private Object actualPartInfo;

            private FieldInfo vesselIDField;

            /// <summary>
            /// Guid of the vessel of the Part
            /// </summary>
            public Guid vesselID
            {
                get { return (Guid)vesselIDField.GetValue(actualPartInfo); }
            }

            private FieldInfo partNameField;

            /// <summary>
            /// The Part Name
            /// </summary>
            public string partName
            {
                get { return (string)partNameField.GetValue(actualPartInfo); }
            }

            private FieldInfo numSeatsField;

            /// <summary>
            /// Number of Seats
            /// </summary>
            public int numSeats
            {
                get { return (int)numSeatsField.GetValue(actualPartInfo); }
            }

            private FieldInfo numCrewField;

            /// <summary>
            /// Number of Crew
            /// </summary>
            public int numCrew
            {
                get { return (int)numCrewField.GetValue(actualPartInfo); }
                set { numCrewField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo crewMembersField;

            /// <summary>
            /// List of Crew Members names
            /// </summary>
            public List<string> crewMembers
            {
                get { return (List<string>)crewMembersField.GetValue(actualPartInfo); }
                set { crewMembersField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo crewMemberTraitsField;

            /// <summary>
            /// List of Crew Members traits
            /// </summary>
            public List<string> crewMemberTraits
            {
                get { return (List<string>)crewMemberTraitsField.GetValue(actualPartInfo); }
                set { crewMemberTraitsField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo numFrznCrewField;

            /// <summary>
            /// Number of Frozen Crew
            /// </summary>
            public int numFrznCrew
            {
                get { return (int)numFrznCrewField.GetValue(actualPartInfo); }
                set { numFrznCrewField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo hibernatingField;

            /// <summary>
            /// Is the vessel hibernating
            /// </summary>
            public bool hibernating
            {
                get { return (bool)hibernatingField.GetValue(actualPartInfo); }
            }

            private FieldInfo hasExternalDoorField;

            /// <summary>
            /// If there's an external door
            /// </summary>
            public bool hasExternalDoor
            {
                get { return (bool)hasExternalDoorField.GetValue(actualPartInfo); }
            }

            private FieldInfo hasExternalPodField;

            /// <summary>
            /// If there's an external pod
            /// </summary>
            public bool hasExternalPod
            {
                get { return (bool)hasExternalPodField.GetValue(actualPartInfo); }
            }

            private FieldInfo timeLastElectricityField;
            /// <summary>
            /// Time last EC was taken
            /// </summary>
            public double timeLastElectricity
            {
                get { return (double)timeLastElectricityField.GetValue(actualPartInfo); }
                set { timeLastElectricityField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo frznChargeRequiredField;
            /// <summary>
            /// The amount of EC required to keep a kerbal frozen.
            /// </summary>
            public double frznChargeRequired
            {
                get { return (double)frznChargeRequiredField.GetValue(actualPartInfo); }
                set { frznChargeRequiredField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo timeLastTempCheckField;
            /// <summary>
            /// The time the temperature was last checked.
            /// </summary>
            public double timeLastTempCheck
            {
                get { return (double)timeLastTempCheckField.GetValue(actualPartInfo); }
                set { timeLastTempCheckField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo deathCounterField;
            /// <summary>
            /// The time counter until death occurs after EC runs out.
            /// </summary>
            public double deathCounter
            {
                get { return (double)deathCounterField.GetValue(actualPartInfo); }
                set { deathCounterField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo tmpdeathCounterField;
            /// <summary>
            /// The amount of EC required to keep a kerbal frozen.
            /// </summary>
            public double tmpdeathCounter
            {
                get { return (double)tmpdeathCounterField.GetValue(actualPartInfo); }
                set { tmpdeathCounterField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo outofECField;
            /// <summary>
            /// If the part freezer is out of EC or not.
            /// </summary>
            public bool outofEC
            {
                get { return (bool)outofECField.GetValue(actualPartInfo); }
                set { outofECField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo TmpStatusField;
            /// <summary>
            /// The freezer temperature status.
            /// </summary>
            public FrzrTmpStatus TmpStatus
            {
                get { return (FrzrTmpStatus)TmpStatusField.GetValue(actualPartInfo); }
                set { TmpStatusField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo ECWarningField;
            /// <summary>
            /// If the part freezer is out of EC or not.
            /// </summary>
            public bool ECWarning
            {
                get { return (bool)ECWarningField.GetValue(actualPartInfo); }
                set { ECWarningField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo TempWarningField;
            /// <summary>
            /// If a temperature warning is in effect.
            /// </summary>
            public bool TempWarning
            {
                get { return (bool)TempWarningField.GetValue(actualPartInfo); }
                set { TempWarningField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo cabinTempField;
            /// <summary>
            /// The cabin temperature.
            /// </summary>
            public float cabinTempRequired
            {
                get { return (float)cabinTempField.GetValue(actualPartInfo); }
                set { cabinTempField.SetValue(actualPartInfo, value); }
            }

            private FieldInfo lastUpdateField;

            /// <summary>
            /// Last time updated
            /// </summary>
            public double lastUpdate
            {
                get { return (double)lastUpdateField.GetValue(actualPartInfo); }
                set { lastUpdateField.SetValue(actualPartInfo, value); }
            }
        }


        #region Logging Stuff

        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(String Message, params Object[] strParams)
        {
            LogFormatted(Message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(String Message, params Object[] strParams)
        {
            Message = String.Format(Message, strParams);
            String strMessageLine = String.Format("{0},{2}-{3},{1}",
                DateTime.Now, Message, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            UnityEngine.Debug.Log(strMessageLine);
        }

        #endregion Logging Stuff
    }
}
