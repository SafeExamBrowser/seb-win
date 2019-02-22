using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//
//  SEBGlobalConstants.cs
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

namespace SebWindowsClient.ConfigurationUtils
{
    public class SEBGlobalConstants
    {
        #region Constants

        // Error levels
        public const int ERROR       = 0;
        public const int WARNING     = 1;
        public const int INFORMATION = 2;
        public const int QUESTION    = 3;

        public const string HKCU = "HKEY_CURRENT_USER";
        public const string HKLM = "HKEY_LOCAL_MACHINE";

        // Strings for registry keys
        public const string KEY_POLICIES_SYSTEM   = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        public const string KEY_POLICIES_EXPLORER = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer";
        public const string KEY_POLICIES_SEB      = "Software\\Policies\\SEB";
        public const string KEY_UTILMAN_EXE       = "Software\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\Utilman.exe";
        public const string KEY_VM_WARE_CLIENT    = "Software\\VMware, Inc.\\VMware VDM\\Client";

        // Strings for registry values
        public const string VAL_HIDE_FAST_USER_SWITCHING = "HideFastUserSwitching";
        public const string VAL_DISABLE_LOCK_WORKSTATION = "DisableLockWorkstation";
        public const string VAL_DISABLE_CHANGE_PASSWORD  = "DisableChangePassword";
        public const string VAL_DISABLE_TASK_MANAGER     = "DisableTaskMgr";
        public const string VAL_NO_LOG_OFF               = "NoLogoff";
        public const string VAL_NO_CLOSE                 = "NoClose";
        public const string VAL_ENABLE_EASE_OF_ACCESS    = "Debugger";
        public const string VAL_ENABLE_SHADE             = "EnableShade";

        // Aligned strings for printing out registry values
        public const string MSG_HIDE_FAST_USER_SWITCHING = "HideFastUserSwitching ";
        public const string MSG_DISABLE_LOCK_WORKSTATION = "DisableLockWorkstation";
        public const string MSG_DISABLE_CHANGE_PASSWORD  = "DisableChangePassword ";
        public const string MSG_DISABLE_TASK_MANAGER     = "DisableTaskMgr        ";
        public const string MSG_NO_LOG_OFF               = "NoLogoff              ";
        public const string MSG_NO_CLOSE                 = "NoClose               ";
        public const string MSG_ENABLE_EASE_OF_ACCESS    = "Debugger              ";
        public const string MSG_ENABLE_SHADE             = "EnableShade           ";

        // Only for trunk version necessary (XULrunner)
        public const string VAL_PERMITTED_APPLICATIONS       = "PermittedApplications";
        public const string VAL_SHOW_SEB_APPLICATION_CHOOSER = "ShowSebApplicationChooser";

        // Languages
        public const int IND_LANGUAGE_MIN = 0;
        public const int IND_LANGUAGE_GERMAN  = 0;
        public const int IND_LANGUAGE_ENGLISH = 1;
        public const int IND_LANGUAGE_FRENCH  = 2;
        public const int IND_LANGUAGE_MAX = 2;
        public const int IND_LANGUAGE_NUM = 3;

        public const int IND_MESSAGE_TEXT_MIN = 0;

        // Error codes
        public const int IND_FILE_NOT_FOUND       = 0;
        public const int IND_PATH_NOT_FOUND       = 1;
        public const int IND_ACCESS_DENIED        = 2;
        public const int IND_UNDEFINED_ERROR      = 3;
        public const int IND_NO_WRITE_PERMISSION  = 4;
        public const int IND_SEB_CLIENT_SEB_ERROR = 5;
        public const int IND_CONFIG_JSON_ERROR    = 6;
        public const int IND_NO_CLIENT_INFO_ERROR = 7;
        public const int IND_INITIALISE_ERROR     = 8;
        public const int IND_REGISTRY_EDIT_ERROR  = 9;
        public const int IND_NOT_ENOUGH_REGISTRY_RIGHTS_ERROR = 10;
        public const int IND_REGISTRY_WARNING                 = 11;
        public const int IND_PROCESS_CALL_FAILED              = 12;
        public const int IND_PROCESS_WINDOW_NOT_FOUND         = 13;
        public const int IND_LOAD_LIBRARY_ERROR               = 14;
        public const int IND_NO_LANGUAGE_STRING_FOUND         = 15;
        public const int IND_NO_INSTANCE                      = 16;
        public const int IND_NO_FILE_ERROR                    = 17;
        public const int IND_NO_TASKBAR_HANDLE                = 18;
        public const int IND_FIREFOX_START_FAILED             = 19;
        public const int IND_KEY_LOGGER_FAILED                = 20;
        public const int IND_KIOX_TERMINATED                  = 21;
        public const int IND_SEB_TERMINATED                   = 22;
        public const int IND_NO_OS_SUPPORT                    = 23;
        public const int IND_KILL_PROCESS_FAILED              = 24;
        public const int IND_VIRTUAL_MACHINE_FORBIDDEN        = 25;
        public const int IND_CLOSE_PROCESS_FAILED             = 26;
        public const int IND_WINDOWS_SERVICE_NOT_AVAILABLE    = 27;
        public const int IND_CLOSE_SEB_FAILED                 = 28;
        public const int IND_MESSAGE_TEXT_MAX = 28;
        public const int IND_MESSAGE_TEXT_NUM = 29;


        // MessageBox supports errors and warnings
        public const int IND_MESSAGE_KIND_ERROR    = 0;
        public const int IND_MESSAGE_KIND_WARNING  = 1;
        public const int IND_MESSAGE_KIND_QUESTION = 2;
        public const int IND_MESSAGE_KIND_NUM      = 3;


        public const int  OS_UNKNOWN =  800;
        public const int  WIN_95     =  950;
        public const int  WIN_98     =  980;
        public const int  WIN_ME     =  999;
        public const int  WIN_NT_351 = 1351;
        public const int  WIN_NT_40  = 1400;
        public const int  WIN_2000   = 2000;
        public const int  WIN_XP     = 2010;
        public const int WIN_VISTA   = 2050;
        public const int WIN_7       = 2050;
        public const int WIN_8       = 2050;

        #endregion
    }
}
