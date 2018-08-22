using System;
using Microsoft.Win32;

namespace SebWindowsServiceWCF.RegistryHandler
{
	public static class RegistryEntryExtensions
	{
		public static void Delete(this RegistryEntry entry)
		{
			var key = entry.RegistryPath.StartsWith("HKEY_USERS") ? Registry.Users : Registry.LocalMachine;

			key = key.OpenSubKey(entry.RegistryPath.Substring(entry.RegistryPath.IndexOf('\\') + 1), true);

			if (key != null)
			{
				key.DeleteValue(entry.DataItemName);
			}
		}

		public static object GetValue(this RegistryEntry entry)
		{
			return Registry.GetValue(entry.RegistryPath, entry.DataItemName, null);
		}

		public static void SetValue(this RegistryEntry entry, object value)
		{
			if (value != null && value.GetType() == entry.DataType)
			{
				Registry.SetValue(entry.RegistryPath, entry.DataItemName, value);
			}
			else
			{
				throw new ArgumentException($"Can't set registry key '{entry.RegistryPath}\\{entry.DataItemName}' to '{value ?? "<NULL>"}' ({value?.GetType()})!");
			}
		}
	}
}
