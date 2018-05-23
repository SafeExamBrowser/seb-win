using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace SebWindowsServiceWCF.ServiceImplementations
{
	/// <summary>
	/// Static implementation to convert a local username to the corresponding SID-Value where the registry keys are stored under HKEY_USERS
	/// </summary>
	public static class SIDHandler
    {
		private const string SID_REGEX_PATTERN = @"S-\d(-\d+)+";

		public static string GetSIDFromUsername(string username)
        {
			var strategies = new Func<string, string>[] { SidFromDotNetFcl, SidFromNativeApi, SidFromWmi, SidFromWmiWbem };

			foreach (var strategy in strategies)
			{
				try
				{
					var sid = strategy.Invoke(username);

					if (IsValid(sid))
					{
						Logger.Log($"Found SID '{sid}' via '{strategy.Method.Name}' for username '{username}'!");

						return sid;
					}
					else
					{
						Logger.Log($"Retrieved invalid SID '{sid}' via '{strategy.Method.Name}' for username '{username}'!");
					}
				}
				catch (Exception e)
				{
					Logger.Log(e, $"Failed to get SID via '{strategy.Method.Name}' for username '{username}'!");
				}
			}

			Logger.Log($"Completely failed to retrieve SID for username '{username}'!");

			return default(string);
        }

		private static string SidFromDotNetFcl(string username)
		{
			var account = new NTAccount(username);
			var sid = (SecurityIdentifier) account.Translate(typeof(SecurityIdentifier));

			return sid.ToString();
		}

		/// <summary>The method converts object name (user, group) into SID string.</summary>
		/// <param name="username">Object name in form domain\object_name.</param>
		/// <returns>SID string.</returns>
		private static string SidFromNativeApi(string username)
        {
            IntPtr _sid = IntPtr.Zero; //pointer to binary form of SID string.
            int _sidLength = 0;   //size of SID buffer.
            int _domainLength = 0;  //size of domain name buffer.
            int _use;     //type of object.
            StringBuilder _domain = new StringBuilder(); //stringBuilder for domain name.
            int _error = 0;
            string _sidString = "";

            //first call of the function only returns the sizes of buffers (SDI, domain name)
            LookupAccountName(null, username, _sid, ref _sidLength, _domain, ref _domainLength, out _use);
            _error = Marshal.GetLastWin32Error();

            if (_error != 122) //error 122 (The data area passed to a system call is too small) - normal behaviour.
            {
                throw (new Exception(new Win32Exception(_error).Message));
            }
            else
            {
                _domain = new StringBuilder(_domainLength); //allocates memory for domain name
                _sid = Marshal.AllocHGlobal(_sidLength); //allocates memory for SID
                bool _rc = LookupAccountName(null, username, _sid, ref _sidLength, _domain, ref _domainLength, out _use);

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

		private static string SidFromWmi(string username)
		{
			var process = new Process();

			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.Arguments = string.Format("/c \"wmic useraccount where name='{0}' get sid\"", username);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.CreateNoWindow = true;
			process.Start();

			var output = process.StandardOutput.ReadToEnd();

			process.WaitForExit(5000);

			var match = Regex.Match(output, SID_REGEX_PATTERN);

			if (match.Success)
			{
				return match.Value;
			}

			return null;
		}

		private static string SidFromWmiWbem(string username)
		{
			var process = new Process();

			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.Arguments = string.Format("/c \"wmic useraccount where name='{0}' get sid\"", username);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WorkingDirectory = Path.Combine(Environment.SystemDirectory, @"wbem\");
			process.Start();

			var output = process.StandardOutput.ReadToEnd();

			process.WaitForExit(5000);

			var match = Regex.Match(output, SID_REGEX_PATTERN);

			if (match.Success)
			{
				return match.Value;
			}

			return null;
		}

		private static bool IsValid(string sid)
		{
			return !String.IsNullOrWhiteSpace(sid) && Regex.IsMatch(sid, $"^{SID_REGEX_PATTERN}$");
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool LookupAccountName([In, MarshalAs(UnmanagedType.LPTStr)] string systemName, [In, MarshalAs(UnmanagedType.LPTStr)] string accountName, IntPtr sid, ref int cbSid, StringBuilder referencedDomainName, ref int cbReferencedDomainName, out int use);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool ConvertSidToStringSid(IntPtr sid, [In, Out, MarshalAs(UnmanagedType.LPTStr)] ref string pStringSid);
	}
}
