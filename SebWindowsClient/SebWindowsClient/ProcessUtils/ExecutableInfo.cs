using System;

namespace SebWindowsClient.ProcessUtils
{
	public class ExecutableInfo
	{
		public string Name { get; }
		public string OriginalName { get; }

		public ExecutableInfo(string name, string originalName = null)
		{
			Name = name ?? string.Empty;
			OriginalName = originalName ?? string.Empty;
		}

		public bool HasName
		{
			get { return !String.IsNullOrEmpty(Name); }
		}

		public bool HasOriginalName
		{
			get { return !String.IsNullOrEmpty(OriginalName); }
		}
	}
}
