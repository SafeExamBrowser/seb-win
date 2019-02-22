using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using SebWindowsServiceWCF.CommandExecutor;
using SebWindowsServiceWCF.RegistryHandler;
using SebWindowsServiceWCF.ServiceImplementations;

//
//  SEBConfigFileManager.cs
//  SafeExamBrowser
//
//  Copyright (c) 2010-2019 Pascal Wyss, 
//  ETH Zurich, Educational Development and Technology (LET),
//  based on the original idea of Safe Exam Browser
//  by Stefan Schneider, University of Giessen
//  Project concept: Thomas Piendl, Daniel R. Schneider,
//  Dirk Bauer, Kai Reuter, Tobias Halbherr, Karsten Burger, Marco Lehre,
//  Brigitte Schmucki, Oliver Rahs. French localization: Nicolas Dunand
//
//  ``The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License at
//  http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
//  License for the specific language governing rights and limitations
//  under the License.
//
//  The Original Code is Safe Exam Browser for Windows.
//
//  The Initial Developer of the Original Code is Pascal Wyss.
//  Portions created by Pascal Wyss
//  are Copyright (c) 2010-2019 Pascal Wyss, 
//  ETH Zurich, Educational Development and Technology (LET), 
//  based on the original idea of Safe Exam Browser
//  by Stefan Schneider, University of Giessen. All Rights Reserved.
//
//  Contributor(s): ______________________________________.
//

namespace SebRegistryResetter
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Initialize();

			CheckUserPrivileges();
			TryResetUsingBackupFile();
			CheckIfProblemSolved();
			TryResetUsingUsername();

			Terminate();
		}

		private static void Initialize()
		{
			Logger.Log("/////////////////////////////////////////////////");
			Logger.Log("// Registry Resetter");
			Logger.Log("// -----------------");

			Console.CancelKeyPress += (o, args) => Terminate();
		}

		private static void CheckUserPrivileges()
		{
			try
			{
				Log("Trying to check user privileges...");

				var identity = WindowsIdentity.GetCurrent();
				var principal = new WindowsPrincipal(identity);
				var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

				Log($"User is {(isAdmin ? string.Empty : "not an ")}administrator.");

				if (!isAdmin)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("This application must be run with administrator privileges!");
					Console.ReadKey();

					Terminate();
				}
			}
			catch (Exception e)
			{
				Log(e, "Failed to check user privileges!");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Unable to determine if this application is running with administrator privileges.");
				Console.WriteLine("Did you run it with administrator privileges? (Y=Yes,N=No)");

				var ranAsAdmin = Console.ReadLine();

				if (ranAsAdmin == null || ranAsAdmin.ToLower() != "y")
				{
					Terminate();
				}

				Log("User claims the application is running as administrator, continuing...");
			}

			Console.WriteLine("Please note:");
			Console.WriteLine("In order to be able to successfully reset all registry changes, the same user under which " +
				"SEB was last running needs to be logged in. Otherwise, all user-specific values cannot be reset. Furthermore, please " +
				"restart the computer after executing the Registry Resetter to ensure that the changes take effect.");
			Console.WriteLine();
		}

		private static void TryResetUsingBackupFile()
		{
			OutputAndLog("Trying to find registry file to reset...");

			try
			{
				var filePath = String.Format(@"{0}\sebregistry.srg", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

				if (File.Exists(filePath))
				{
					OutputAndLog(String.Format("Found {0}", filePath));
					OutputAndLog("Resetting Registry keys...");

					var service = new RegistryService();
					var success = service.Reset();

					UpdateGroupPolicies();

					if (success)
					{
						Console.ForegroundColor = ConsoleColor.Green;
						OutputAndLog("Registry keys were successfully reset!");
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						OutputAndLog("Unable to reset all registry keys!");
					}
				}
				else
				{
					OutputAndLog("File not found!");
				}
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				OutputAndLog(String.Format("Error: Unable to find file or reset registry keys\n{0}:{1}", ex.ToString(), ex.Message));
			}
		}

		private static void CheckIfProblemSolved()
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Is there anything NOT working as expected when you press CTRL+ALT+DEL? (Y=Yes/N=No)");

			var response = Console.ReadLine();

			if (response?.ToLower() != "y")
			{
				Log("User declared that everything is working as expected.");

				Terminate();
			}
		}

		private static void TryResetUsingUsername()
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Under what user did you run the SEB Windows Client? (Please type in the username followed by ENTER)");

			var username = Console.ReadLine();
			var sid = SIDHandler.GetSIDFromUsername(username);

			while (string.IsNullOrWhiteSpace(sid))
			{
				Console.WriteLine("SID for username '{0}' could not be found in the registry! Try again or hit CTRL+C / CTRL+Z to exit...", username);
				Log($"Failed to retrieve SID for input '{username}'!");

				username = Console.ReadLine();
				sid = SIDHandler.GetSIDFromUsername(username);
			}

			Console.ResetColor();
			OutputAndLog(String.Format("Username: {0} / SID: {1}", username, sid));

			SetDefaultValues(sid);
			UpdateGroupPolicies();

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Please note that Remote Desktop Connections have been disabled. They can be re-activated in the system settings, if needed.");
			Console.WriteLine("Finished, press any key to exit the application");
			Console.ReadKey();
		}

		private static void SetDefaultValues(string sid)
		{
			var entriesToBeSetToZero = new List<RegistryEntry>
			{
				new RegDisableChangePassword(sid),
				new RegDisableLockWorkstation(sid),
				new RegDisableTaskMgr(sid),
				new RegHideFastUserSwitching(sid),
				new RegNoClose(sid),
				new RegNoCloseWin7(sid),
				new RegNoLogoff(sid),
				new RegDontDisplayNetworkSelectionUI(sid),
				new RegNoAutoRebootWithLoggedOnUsers(sid),
				new RegEaseOfAccess(sid)
			};
			var entriesToBeSetToOne = new List<RegistryEntry>
			{
				new RegEnableShade(sid)
			};

			OutputAndLog("Resetting all registry entries possibly touched by the SEB Windows Service to an unrestricted value...");

			foreach (var entry in entriesToBeSetToZero)
			{
				Reset(entry, 0);
			}

			foreach (var entry in entriesToBeSetToOne)
			{
				Reset(entry, 1);
			}

			Reset(new RegEnableShadeHorizon(sid), "True");
		}

		private static void UpdateGroupPolicies()
		{
			OutputAndLog("Starting group policy update...");
			new CommandExecutor().ExecuteCommandSync("gpupdate /force");
			OutputAndLog("Finished group policy update.");
		}

		private static void Reset(RegistryEntry entry, object value)
		{
			try
			{
				if (entry.IsUserSpecific() && !entry.IsHiveAvailable())
				{
					OutputAndLog($"User hive is not available, cannot reset registry key '{entry}'!");
				}
				else
				{
					entry.SetValue(value);
					Console.ForegroundColor = ConsoleColor.Green;
					OutputAndLog($@"Successfully set '{entry}' to '{value}'");
				}
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				OutputAndLog($@"Failed to set '{entry}' to '{value}'");
				Log(e);
			}
		}

		private static void OutputAndLog(string message)
		{
			Console.WriteLine(message);
			Log(message);
		}

		private static void Log(Exception e, string message = null)
		{
			Logger.Log(e, $"// {message}");
		}

		private static void Log(string message)
		{
			Logger.Log($"// {message}");
		}

		private static void Terminate()
		{
			Logger.Log("/////////////////////////////////////////////////");
			Environment.Exit(0);
		}
	}
}
