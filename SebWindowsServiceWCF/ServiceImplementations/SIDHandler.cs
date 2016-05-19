using System;
using System.Security.Principal;

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
                var account = new NTAccount(username);
                var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
                return sid.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, String.Format("Unable to get SID for username {0}",username));
                return "";
            }
        }
    }
}
