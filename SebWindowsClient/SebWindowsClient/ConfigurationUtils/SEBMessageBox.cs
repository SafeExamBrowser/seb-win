using System;
using System.Windows.Forms;
//
//  SEBMessageBox.cs
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
using MetroFramework;

namespace SebWindowsClient.ConfigurationUtils
{
	public class SEBMessageBox
    {        
        // **********************************
        // Output an error or warning message
        // **********************************
        public static DialogResult Show(string messageTitle, string messageText, MessageBoxIcon messageBoxIcon, MessageBoxButtons messageButtons, bool neverShowTouchOptimized = false)
        {
            // If we are running in SebWindowsClient we need to activate it before showing the password dialog
            if (SEBClientInfo.SebWindowsClientForm != null) SebWindowsClientMain.SEBToForeground();

            DialogResult messageBoxResult;
            if (!neverShowTouchOptimized && (Boolean) SEBClientInfo.getSebSetting(SEBSettings.KeyTouchOptimized)[SEBSettings.KeyTouchOptimized] == true)
            {
				var owner = SEBClientInfo.SebWindowsClientForm ?? new Form()
				{
					TopMost = true,
					Top = 0,
					Left = 0,
					Width = Screen.PrimaryScreen.Bounds.Width,
					Height = Screen.PrimaryScreen.Bounds.Height
				};

				messageBoxResult = MetroMessageBox.Show(owner, messageText, messageTitle, messageButtons, messageBoxIcon);
            }
            else
            {
                messageBoxResult = MessageBox.Show(new Form() { TopMost = true }, messageText, messageTitle, messageButtons, messageBoxIcon);
            }

            return messageBoxResult;
        }
    }
}
