using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace SebWindowsServiceWCF.ServiceImplementations
{
    /// <summary>
    /// Static implementation to convert a local username to the corresponding SID-Value where the registry keys are stored under HKEY_USERS
    /// </summary>
    public static class SIDHandler
    {
        /// <summary>
        /// Returns the SID of the user with the username
        /// Throws an exception of something does not work
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>SID as String</returns>
        public static string GetSIDFromUsername(string username)
        {
            try
            {
                return GetSid(username);
            }
            catch (Exception ex)
            {
                try
                {
                    //Alternative Method
                    var account = new NTAccount(username);
                    var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
                    var sidString = sid.ToString();
                    if (string.IsNullOrWhiteSpace(sidString))
                    {
                        throw new Exception("SID Empty");
                    }
                    return sid.ToString();
                }
                catch (Exception)
                {
                    Logger.Log(ex, String.Format("Unable to get SID for username {0}", username));
                    return "";
                }
            }
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool LookupAccountName([In, MarshalAs(UnmanagedType.LPTStr)] string systemName, [In, MarshalAs(UnmanagedType.LPTStr)] string accountName, IntPtr sid, ref int cbSid, StringBuilder referencedDomainName, ref int cbReferencedDomainName, out int use);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool ConvertSidToStringSid(IntPtr sid, [In, Out, MarshalAs(UnmanagedType.LPTStr)] ref string pStringSid);


        /// <summary>The method converts object name (user, group) into SID string.</summary>
        /// <param name="name">Object name in form domain\object_name.</param>
        /// <returns>SID string.</returns>
        private static string GetSid(string name)
        {
            IntPtr _sid = IntPtr.Zero; //pointer to binary form of SID string.
            int _sidLength = 0;   //size of SID buffer.
            int _domainLength = 0;  //size of domain name buffer.
            int _use;     //type of object.
            StringBuilder _domain = new StringBuilder(); //stringBuilder for domain name.
            int _error = 0;
            string _sidString = "";

            //first call of the function only returns the sizes of buffers (SDI, domain name)
            LookupAccountName(null, name, _sid, ref _sidLength, _domain, ref _domainLength, out _use);
            _error = Marshal.GetLastWin32Error();

            if (_error != 122) //error 122 (The data area passed to a system call is too small) - normal behaviour.
            {
                throw (new Exception(new Win32Exception(_error).Message));
            }
            else
            {
                _domain = new StringBuilder(_domainLength); //allocates memory for domain name
                _sid = Marshal.AllocHGlobal(_sidLength); //allocates memory for SID
                bool _rc = LookupAccountName(null, name, _sid, ref _sidLength, _domain, ref _domainLength, out _use);

                if (_rc == false)
                {
                    _error = Marshal.GetLastWin32Error();
                    Marshal.FreeHGlobal(_sid);
                    throw (new Exception(new Win32Exception(_error).Message));
                }
                else
                {
                    // converts binary SID into string
                    _rc = ConvertSidToStringSid(_sid, ref _sidString);

                    if (_rc == false)
                    {
                        _error = Marshal.GetLastWin32Error();
                        Marshal.FreeHGlobal(_sid);
                        throw (new Exception(new Win32Exception(_error).Message));
                    }
                    else
                    {
                        Marshal.FreeHGlobal(_sid);
                        return _sidString;
                    }
                }
            }
        }
    }
}
