using System.Collections.Generic;
using System.ServiceModel;

namespace SEBWindowsServiceContracts
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
        bool SetRegistryEntries(Dictionary<RegistryIdentifiers, object> registryValues, string sid, string username);

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
		DisableChromeNotifications,
        DisableLockWorkstation,
        DisableTaskMgr,
        DisableChangePassword,
        HideFastUserSwitching,
        NoLogoff,
        NoClose,
		NoCloseWin7,
        EnableShade,
        EnableShadeHorizon,
        EaseOfAccess,
        DontDisplayNetworkSelectionUI,
        NoAutoRebootWithLoggedOnUsers,
        fDenyTSConnections
    }

}
