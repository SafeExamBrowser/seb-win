using System;
using System.Configuration;
using Microsoft.Win32;
using SebWindowsServiceWCF.ServiceImplementations;

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
        protected string SID;
        //protected RegistryKey RegistryKey;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SID">The SID of the user for which the registry entry should be changed (SubKey of HK_USERS)</param>
        protected RegistryEntry(string SID)
        {
            this.SID = SID;
        }

        /// <summary>
        /// The Value of the registryentry
        /// Throws an Exception if something does not work
        /// </summary>
        public object DataValue
        {
            get
            {
                try
                {
                    //Returns null if the key is inexistent
                    return Registry.GetValue(this.RegistryPath, this.DataItemName, null);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, String.Format("Unable to get registry entry for key: {0} and value {1}",this.RegistryPath, this.DataItemName));
                    throw;
                }
            }

            set
            {
                try
                {
                    //If the value to be set is null (which means: delete the value) and the key exists -> delete it
                    if (value == null && DataValue != null)
                    {
                        try
                        {
                            //Get the Root Key (either HKEY_USERS or HKEY_LOCAL_MACHINE
                            var regKey = this.RegistryPath.StartsWith("HKEY_USERS") ? Registry.Users : Registry.LocalMachine;
                            //Load the subkey
                            regKey = regKey.OpenSubKey(RegistryPath.Substring(this.RegistryPath.IndexOf('\\') + 1), true);
                            //If the subkey exists, delete the value
                            if (regKey != null)
                                regKey.DeleteValue(this.DataItemName);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, String.Format("Unable to delete the registry value {0} in the registry key {1}",this.DataItemName,this.RegistryPath));
                            throw;
                        }
                        
                    }
                    //Set the value if it is of the correct type and is not null
                    else if (value != null && value.GetType() == this.DataType)
                    {
                        Registry.SetValue(this.RegistryPath, this.DataItemName, value);
                    }
                    else
                    {
                        Logger.Log(String.Format("Unable to set the value {2}:{0} for the registrykey {1}, because the value is of the wrong type", value, this.RegistryPath, this.DataItemName));
                        throw new SettingsPropertyWrongTypeException("The value is of the wrong type");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, String.Format("Unable to set the value {2}:{0} (null means delete) for the registrykey {1}", value, this.RegistryPath, this.DataItemName));
                    throw;
                }
            }
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
            this.RegistryPath = String.Format(@"HKEY_USERS\{0}\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",this.SID);
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
}
