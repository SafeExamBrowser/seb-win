using System;

namespace SebWindowsServiceWCF.RegistryHandler
{
	/// <summary>
	/// Abstract Parent Class Registry Entry for specific RegistryEntries
	/// </summary>
	public abstract class RegistryEntry
    {
        /// <summary>
        /// The complete path to the registry key starting with HKEY_[...]
        /// </summary>
        public string RegistryPath
        { get; protected set; }
        /// <summary>
        /// The name of the inside of the registry key
        /// </summary>
        public string DataItemName
        { get; protected set; }
        /// <summary>
        /// The datatype of the value (e.g. REG_DWORD = Int32, REG_SZ = String)
        /// </summary>
        public Type DataType
        { get; protected set; }
        /// <summary>
        /// The SID of the user for which the registry entries should be changed (SubKey of HK_USERS)
        /// </summary>
        public string SID { get; protected set; }
        //protected RegistryKey RegistryKey;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SID">The SID of the user for which the registry entry should be changed (SubKey of HK_USERS)</param>
        protected RegistryEntry(string SID)
        {
            this.SID = SID;
        }

		public override string ToString()
		{
			return $@"{RegistryPath}\{DataItemName}";
		}
	}

	/// <remarks>
	/// IMPORTANT: This registry configuration only has an effect after Chrome is restarted!
	/// 
	/// See https://www.chromium.org/administrators/policy-list-3#DefaultNotificationsSetting:
	/// •	1 = Allow sites to show desktop notifications
	/// •	2 = Do not allow any site to show desktop notifications
	/// •	3 = Ask every time a site wants to show desktop notifications
	/// </remarks>
	public class RegDisableChromeNotifications : RegistryEntry
	{
		public RegDisableChromeNotifications(string SID) : base(SID)
		{
			RegistryPath = String.Format(@"HKEY_USERS\{0}\Software\Policies\Google\Chrome", SID);
			DataItemName = "DefaultNotificationsSetting";
			DataType = typeof(Int32);
		}
	}

    public class RegDisableLockWorkstation : RegistryEntry
    {
        public RegDisableLockWorkstation(string SID) : base(SID)
        {
            this.RegistryPath = String.Format(@"HKEY_USERS\{0}\Software\Microsoft\Windows\CurrentVersion\Policies\System",this.SID);
            this.DataItemName = "DisableLockWorkstation";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }

    public class RegDisableTaskMgr : RegistryEntry
    {
        public RegDisableTaskMgr(string SID) : base(SID)
        {
            this.RegistryPath = String.Format(@"HKEY_USERS\{0}\Software\Microsoft\Windows\CurrentVersion\Policies\System", this.SID);
            this.DataItemName = "DisableTaskMgr";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }

    public class RegDisableChangePassword : RegistryEntry
    {
        public RegDisableChangePassword(string SID) : base(SID)
        {
            this.RegistryPath = String.Format(@"HKEY_USERS\{0}\Software\Microsoft\Windows\CurrentVersion\Policies\System",this.SID);
            this.DataItemName = "DisableChangePassword";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }
    public class RegHideFastUserSwitching : RegistryEntry
    {
        public RegHideFastUserSwitching(string SID) : base(SID)
        {
            this.RegistryPath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\System";
            this.DataItemName = "HideFastUserSwitching";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }
    public class RegNoLogoff : RegistryEntry
    {
        public RegNoLogoff(string SID) : base(SID)
        {
            this.RegistryPath = String.Format(@"HKEY_USERS\{0}\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",this.SID);
            this.DataItemName = "NoLogoff";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }
    
    public class RegNoClose : RegistryEntry
    {
        public RegNoClose(string SID) : base(SID)
        {
            this.RegistryPath = String.Format(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", this.SID);
            this.DataItemName = "NoClose";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }

	public class RegNoCloseWin7 : RegistryEntry
	{
		public RegNoCloseWin7(string SID) : base(SID)
		{
			this.RegistryPath = String.Format(@"HKEY_USERS\{0}\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", this.SID);
			this.DataItemName = "NoClose";
			//DWORD
			this.DataType = typeof(Int32);
		}
	}

	public class RegEnableShade : RegistryEntry
    {
        public RegEnableShade(string SID) : base(SID)
        {
            this.RegistryPath = String.Format(@"HKEY_USERS\{0}\Software\VMware, Inc.\VMware VDM\Client", this.SID);
            this.DataItemName = "EnableShade";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }

    public class RegEnableShadeHorizon : RegistryEntry
    {
        public RegEnableShadeHorizon(string SID) : base(SID)
        {
            this.RegistryPath = String.Format(@"HKEY_USERS\{0}\Software\Policies\VMware, Inc.\VMware VDM\Client", this.SID);
            this.DataItemName = "EnableShade";
            //SZ
            this.DataType = typeof(String);
        }
    }
    
    public class RegEaseOfAccess : RegistryEntry
    {
        public RegEaseOfAccess(string SID) : base(SID)
        {
            this.RegistryPath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\Utilman.exe";
            this.DataItemName = "Debugger";
            //SZ
            this.DataType = typeof(String);
        }
    }
    public class RegDontDisplayNetworkSelectionUI : RegistryEntry
    {
        public RegDontDisplayNetworkSelectionUI(string SID)
            : base(SID)
        {
            this.RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System";
            this.DataItemName = "DontDisplayNetworkSelectionUI";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }

    public class RegNoAutoRebootWithLoggedOnUsers : RegistryEntry
    {
        public RegNoAutoRebootWithLoggedOnUsers(string SID)
            : base(SID)
        {
            this.RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";
            this.DataItemName = "NoAutoRebootWithLoggedOnUsers";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }

    public class RegfDenyTSConnections : RegistryEntry
    {
        public RegfDenyTSConnections(string SID)
            : base(SID)
        {
            this.RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server";
            this.DataItemName = "fDenyTSConnections";
            //DWORD
            this.DataType = typeof(Int32);
        }
    }
}
