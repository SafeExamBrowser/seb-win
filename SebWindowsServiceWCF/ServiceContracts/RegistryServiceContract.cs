using System.Collections.Generic;
using System.ServiceModel;

namespace SebWindowsServiceWCF.ServiceContracts
{
    /// <summary>
    /// The contact for the WCF Service
    /// </summary>
    [ServiceContract]
    public interface IRegistryServiceContract
    {
        [OperationContract]
        bool TestServiceConnetcion();

        [OperationContract]
        bool SetRegistryEntries(Dictionary<RegistryIdentifiers, object> registryValues, string username);

        [OperationContract]
        bool Reset();

        [OperationContract]
        bool DisableWindowsUpdate();
    }

    /// <summary>
    /// The possible registry identifiers
    /// </summary>
    /// Don't add a value without creating the corresponding subclass of RegistryEntry
    public enum RegistryIdentifiers
    {
        DisableLockWorkstation,
        DisableTaskMgr,
        DisableChangePassword,
        HideFastUserSwitching,
        NoLogoff,
        NoClose,
        EnableShade,
        EnableShadeHorizon,
        EaseOfAccess
    }

}
