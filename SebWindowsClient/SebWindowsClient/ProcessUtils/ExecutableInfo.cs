using System;

namespace SebWindowsClient.ProcessUtils
{
	public class ExecutableInfo
	{
		public int? ProcessId { get; }
		public string Name { get; }
		public string OriginalName { get; }

		public ExecutableInfo(string name, string originalName = null, int? processId = null)
		{
			Name = name ?? string.Empty;
			OriginalName = originalName ?? string.Empty;
			ProcessId = processId;
		}

		public bool HasName
		{
			get { return !String.IsNullOrEmpty(Name); }
		}

		public bool HasOriginalName
		{
			get { return !String.IsNullOrEmpty(OriginalName); }
		}

		public bool NamesAreEqual
		{
			get { return Name.Equals(OriginalName, StringComparison.InvariantCultureIgnoreCase); }
		}
	}
}