using System;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace SebWindowsServiceWCF.RegistryHandler
{
	public static class RegistryEntryExtensions
	{
		public static bool IsHiveAvailable(this RegistryEntry entry)
		{
			if (entry.IsUserSpecific())
			{
				using (var key = Registry.Users.OpenSubKey(entry.SID))
				{
					return key != null;
				}
			}

			return true;
		}

		public static bool IsUserSpecific(this RegistryEntry entry)
		{
			return entry.RegistryPath.StartsWith("HKEY_USERS");
		}

		public static object GetValue(this RegistryEntry entry)
		{
			return Registry.GetValue(entry.RegistryPath, entry.DataItemName, null);
		}

		public static void SetValue(this RegistryEntry entry, object value)
		{
			if (value != null && entry is RegEaseOfAccess)
			{
				Registry.SetValue(entry.RegistryPath, entry.DataItemName, value as int? == 1 ? "SebDummy.exe" : string.Empty);
			}
			else if (value != null && value.GetType() == entry.DataType)
			{
				Registry.SetValue(entry.RegistryPath, entry.DataItemName, value);
			}
			else
			{
				throw new ArgumentException($"Can't set registry key '{entry.RegistryPath}\\{entry.DataItemName}' to '{value ?? "<NULL>"}' ({value?.GetType()})!");
			}
		}

		public static bool TryDelete(this RegistryEntry entry)
		{
			var root = entry.IsUserSpecific() ? Registry.Users : Registry.LocalMachine;
			var subkey = entry.RegistryPath.Substring(entry.RegistryPath.IndexOf('\\') + 1);

			using (var key = root.OpenSubKey(subkey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl))
			{
				if (key != null)
				{
					key.DeleteValue(entry.DataItemName);
					key.Flush();

					return true;
				}
			}

			return false;
		}
	}
}
