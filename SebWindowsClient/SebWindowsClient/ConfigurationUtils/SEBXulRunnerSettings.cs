//
//  SEBXulRunnerSettings.cs
//  SafeExamBrowser
//
//  Copyright (c) 2010-2019 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, Pascal Wyss,
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
//  The Initial Developers of the Original Code are Viktor Tomas, 
//  Dirk Bauer, Daniel R. Schneider, Pascal Wyss.
//  Portions created by Viktor Tomas, Dirk Bauer, Daniel R. Schneider, Pascal Wyss
//  are Copyright (c) 2010-2019 Viktor Tomas, Dirk Bauer, Daniel R. Schneider, 
//  Pascal Wyss, ETH Zurich, Educational Development and Technology (LET), 
//  based on the original idea of Safe Exam Browser
//  by Stefan Schneider, University of Giessen. All Rights Reserved.
//
//  Contributor(s): ______________________________________.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using SebWindowsClient.CryptographyUtils;
using SebWindowsClient.DiagnosticsUtils;
using SebWindowsClient.XULRunnerCommunication;
using DictObj = System.Collections.Generic.Dictionary<string, object>;
using ListObj = System.Collections.Generic.List<object>;

namespace SebWindowsClient.ConfigurationUtils
{
	public class XULRunnerConfig
	{
		public Prefs prefs = new Prefs();
		public string seb_url;
		public bool seb_mainWindow_titlebar_enabled;
		public bool seb_trusted_content;
		public bool seb_pattern_regex;
		public string seb_blacklist_pattern;
		public string seb_whitelist_pattern;
		public bool seb_locked;
		public string seb_lock_keycode;
		public string seb_lock_modifiers;
		public bool seb_unlock_enabled;
		public string seb_unlock_keycode;
		public string seb_unlock_modifiers;
		public string seb_shutdown_keycode;
		public string seb_shutdown_modifiers;
		public string seb_load;
		public string seb_load_referrer_instring;
		public string seb_load_keycode;
		public string seb_load_modifiers;
		public string seb_reload_keycode;
		public string seb_reload_modifiers;
		public int seb_net_max_times;
		public int seb_net_timeout;
		public int seb_restart_mode;
		public string seb_restart_keycode;
		public string seb_restart_modifiers;
		public bool seb_popupWindows_titlebar_enabled;
		public int seb_openwin_width;
		public int seb_openwin_height;
		public string seb_showall_keycode;
		public bool seb_distinct_popup;
		public bool seb_removeProfile;
	}

	public class Prefs
	{
		public string general_useragent_override;
	}
	/// <summary>
	/// JSON Serialization and Deserialization Assistant Class
	/// </summary>
	public class SEBXulRunnerSettings
	{
		/// <summary>
		/// JSON Serialization
		/// </summary>
		public static void XULRunnerConfigSerialize(XULRunnerConfig objXULRunnerConfig, string path)
		{
			//string json = "{\"prefs\":{\"general.useragent.override\":\"SEB\"},\"seb.url\":\"http://www.safeexambrowser.org\",\"seb.mainWindow.titlebar.enabled\":false,\"seb.trusted.content\":true,\"seb.pattern.regex\":false,\"seb.blacklist.pattern\":\"\",\"seb.whitelist.pattern\":\"\",\"seb.locked\":true,\"seb.lock.keycode\":\"VK_F2\",\"seb.lock.modifiers\":\"controlshift\",\"seb.unlock.enabled\":false,\"seb.unlock.keycode\":\"VK_F3\",\"seb.unlock.modifiers\":\"controlshift\",\"seb.shutdown.keycode\":\"VK_F4\",\"seb.shutdown.modifiers\":\"controlshift\",\"seb.load\":\"\",\"seb.load.referrer.instring\":\"\",\"seb.load.keycode\":\"VK_F6\",\"seb.load.modifiers\":\"controlshift\",\"seb.reload.keycode\":\"VK_F5\",\"seb.reload.modifiers\":\"\",\"seb.net.max.times\":3,\"seb.net.timeout\":10000,\"seb.restart.mode\":2,\"seb.restart.keycode\":\"VK_F9\",\"seb.restart.modifiers\":\"controlshift\",\"seb.popupWindows.titlebar.enabled\":false,\"seb.openwin.width\":800,\"seb.openwin.height\":600,\"seb.showall.keycode\":\"VK_F1\",\"seb.distinct.popup\":false,\"seb.removeProfile\":false}";
			//Serialise 
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			string js = serializer.Serialize(objXULRunnerConfig);
			js = js.Replace("_", ".");
			//Write to config.json
			File.Delete(path);
			FileStream fs = File.Open(path,FileMode.CreateNew);
			StreamWriter sw = new StreamWriter(fs);
			sw.Write(js);
			sw.Close();
			fs.Close();
		}
		/// <summary>
		/// JSON Deserialization
		/// </summary>
		public static XULRunnerConfig XULRunnerConfigDeserialize(string path)
		{
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			FileStream fs = File.OpenRead(path);
			fs.Position = 0;
			StreamReader sr = new StreamReader(fs);
			string sXULRunnerConfig = sr.ReadToEnd();
			sr.Close();
			fs.Close();

			sXULRunnerConfig = sXULRunnerConfig.Replace("\n", String.Empty);
			sXULRunnerConfig = sXULRunnerConfig.Replace("\r", String.Empty);
			sXULRunnerConfig = sXULRunnerConfig.Replace(" ", String.Empty);
			sXULRunnerConfig = sXULRunnerConfig.Replace("\t", String.Empty);
			sXULRunnerConfig = sXULRunnerConfig.Replace(".", "_");

			XULRunnerConfig objXULRunnerConfig = (XULRunnerConfig)serializer.Deserialize(sXULRunnerConfig, typeof(XULRunnerConfig));

			return objXULRunnerConfig;
		}
		/// <summary>
		/// JSON Serialization of Settings Dictionary
		/// </summary>
		public static string XULRunnerConfigDictionarySerialize(Dictionary<string, object> xulRunnerSettings)
		{
			// Add current Browser Exam Key
			if ((bool)xulRunnerSettings[SEBSettings.KeySendBrowserExamKey])
			{
				string browserExamKey = SEBProtectionController.ComputeBrowserExamKey();
				xulRunnerSettings[SEBSettings.KeyBrowserExamKey] = browserExamKey;
				xulRunnerSettings[SEBSettings.KeyBrowserURLSalt] = true;
			}

			//If necessary replace the starturl with the path to the startResource
			xulRunnerSettings[SEBSettings.KeyStartURL] = GetStartupPathOrUrl();

			// Eventually update setting 
			if ((Boolean)SEBSettings.settingsCurrent[SEBSettings.KeyRestartExamUseStartURL] == true) 
			{
				xulRunnerSettings[SEBSettings.KeyRestartExamURL] = xulRunnerSettings[SEBSettings.KeyStartURL];
			}

			// Check if URL filter is enabled and send according keys to XULRunner seb only if it is
			if ((bool)xulRunnerSettings[SEBSettings.KeyURLFilterEnable] == false)
			{
				xulRunnerSettings[SEBSettings.KeyUrlFilterBlacklist] = "";
				xulRunnerSettings[SEBSettings.KeyUrlFilterWhitelist] = "";
			}
			else
			{
                //Add the socket address 
                if (!String.IsNullOrWhiteSpace(xulRunnerSettings[SEBSettings.KeyUrlFilterWhitelist].ToString()))
                {
                    xulRunnerSettings[SEBSettings.KeyUrlFilterWhitelist] += @";";
                }
                //Add the Socket address with http protocoll instead of ws protocoll for the injected iframe
                xulRunnerSettings[SEBSettings.KeyUrlFilterWhitelist] += String.Format("http://{0}", SEBXULRunnerWebSocketServer.ServerAddress.Substring(5));
            }

            // Add websocket sever address to XULRunner seb settings
            xulRunnerSettings[SEBSettings.KeyBrowserMessagingSocket] = SEBXULRunnerWebSocketServer.ServerAddress;
			xulRunnerSettings[SEBSettings.KeyBrowserMessagingSocketEnabled] = true;
			Logger.AddInformation("Socket: " + xulRunnerSettings[SEBSettings.KeyBrowserMessagingSocket].ToString(), null, null);

			// Expand environment variables in paths which XULRunner seb is processing
			string downloadDirectoryWin = (string)xulRunnerSettings[SEBSettings.KeyDownloadDirectoryWin];
			downloadDirectoryWin = Environment.ExpandEnvironmentVariables(downloadDirectoryWin);
			//downloadDirectoryWin = downloadDirectoryWin.Replace(@"\", @"\\");
			xulRunnerSettings[SEBSettings.KeyDownloadDirectoryWin] = downloadDirectoryWin;

			if ((bool)xulRunnerSettings[SEBSettings.KeyTouchOptimized] == true)
			{
                // Switch off XULRunner seb reload warnings
                xulRunnerSettings[SEBSettings.KeyShowReloadWarning] = false;
                xulRunnerSettings[SEBSettings.KeyNewBrowserWindowShowReloadWarning] = false;


                // Set correct taskbar height according to display dpi
                xulRunnerSettings[SEBSettings.KeyTaskBarHeight] = (int)Math.Round((int)xulRunnerSettings[SEBSettings.KeyTaskBarHeight] * 1.7);
			}

            // Add proper browser user agent string to XULRunner seb settings
            xulRunnerSettings[SEBSettings.KeyBrowserUserAgent] = SEBClientInfo.BROWSER_USERAGENT_SEB + "/" + Application.ProductVersion + 
                (String.IsNullOrEmpty((String)xulRunnerSettings[SEBSettings.KeyBrowserUserAgent]) ? "" : " " + xulRunnerSettings[SEBSettings.KeyBrowserUserAgent]);

			// Set onscreen keyboard settings flag when touch optimized is enabled
			xulRunnerSettings[SEBSettings.KeyBrowserScreenKeyboard] = (bool)xulRunnerSettings[SEBSettings.KeyTouchOptimized];

			//Remove all AdditionalResourceData from settings
			RecursivelyRemoveAdditionalResourceData((ListObj)xulRunnerSettings[SEBSettings.KeyAdditionalResources]);

			// The additional dictionary data is being extracted by the .NET-part, the browser only expects a path to the respective folder
			xulRunnerSettings.Remove(SEBSettings.KeyAdditionalDictionaries);

			xulRunnerSettings.Remove(SEBSettings.KeyPermittedProcesses);
			xulRunnerSettings.Remove(SEBSettings.KeyProhibitedProcesses);
			xulRunnerSettings.Remove(SEBSettings.KeyURLFilterRules);

			// The installed operating system culture for correct website localization
			xulRunnerSettings.Add("browserLanguage", System.Globalization.CultureInfo.CurrentCulture.Name);

			// Serialise 
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			string jsonSettings = serializer.Serialize(xulRunnerSettings);
			// Convert to Base64 String
			byte[] bytesJson = Encoding.UTF8.GetBytes(jsonSettings);
			string base64Json = Convert.ToBase64String(bytesJson);
			//// remove the two chars "==" from the end of the string
			//string base64Json = base64String.Substring(0, base64String.Length - 2);

			return base64Json;
		}

		private static string GetStartupPathOrUrl()
		{
			if (!string.IsNullOrEmpty((string)SEBSettings.settingsCurrent[SEBSettings.KeyStartResource]))
			{
				var resource =
					GetAdditionalResourceById((string)SEBSettings.settingsCurrent[SEBSettings.KeyStartResource]);
				var filename = (string)resource[SEBSettings.KeyAdditionalResourcesResourceDataFilename];
				var path =
					new FileCompressor().DecompressDecodeAndSaveFile(
						(string)resource[SEBSettings.KeyAdditionalResourcesResourceData], filename, resource[SEBSettings.KeyAdditionalResourcesIdentifier].ToString());
				return new Uri(path + filename).AbsoluteUri;
			}
			else
			{
				return SEBClientInfo.getSebSetting(SEBSettings.KeyStartURL)[SEBSettings.KeyStartURL].ToString();
			}
		}

		private static DictObj GetAdditionalResourceById(string resourceIdentifier)
		{
			var idPath = resourceIdentifier.Split('.');
			var l0resource =
				(DictObj)((ListObj)SEBSettings.settingsCurrent[SEBSettings.KeyAdditionalResources])[int.Parse(idPath[0])];
			if (idPath.Count() > 1)
			{
				var l1resource = (DictObj)((ListObj)l0resource[SEBSettings.KeyAdditionalResources])[int.Parse(idPath[1])];
				if (idPath.Count() > 2)
				{
					return (DictObj)((ListObj)l1resource[SEBSettings.KeyAdditionalResources])[int.Parse(idPath[2])];
				}
				return l1resource;
			}
			return l0resource;
		}

		private static void RecursivelyRemoveAdditionalResourceData(ListObj additionalResources)
		{
			foreach (DictObj additionalResource in additionalResources)
			{
				additionalResource.Remove(SEBSettings.KeyAdditionalResourcesResourceData);
				additionalResource.Remove(SEBSettings.KeyAdditionalResourcesResourceIcons);
				if (additionalResource[SEBSettings.KeyAdditionalResources] != null)
				{
					RecursivelyRemoveAdditionalResourceData((ListObj)additionalResource[SEBSettings.KeyAdditionalResources]);
				}
			}
		}

	}
}
