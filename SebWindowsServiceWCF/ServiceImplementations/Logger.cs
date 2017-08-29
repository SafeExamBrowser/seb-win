using System;
using System.IO;
using System.Reflection;

namespace SebWindowsServiceWCF.ServiceImplementations
{
	/// <summary>
	/// Static implementation of a file logger
	/// </summary>
	public static class Logger
	{
		private static string _filepath;

		/// <summary>
		/// Logg the content of the exception together with a message
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="content">Message</param>
		public static void Log(Exception ex, string content)
		{
			Log(String.Format("{3} {0}: {1}\n{2}", ex.Message, content, ex.StackTrace, ex.ToString()));
		}

		/// <summary>
		/// Log a message
		/// </summary>
		/// <param name="content">Message</param>
		public static void Log(string content)
		{
			try
			{
				//The logfile is stored where the executable of the service is
				_filepath = String.Format(@"{0}\sebwindowsservice.log", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

				using (var file = new StreamWriter(_filepath, true))
				{
					file.WriteLine(String.Format("{1}: {0}", content, DateTime.Now.ToLocalTime()));
				}
			}
			//If unable to log, you're lost...
			catch { }
		}
	}
}
