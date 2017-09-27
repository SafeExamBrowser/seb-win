using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using SebWindowsClient.ConfigurationUtils;

//
//  Dialog.cs
//  SafeExamBrowser
//
//  Copyright (c) 2010-2016 Daniel R. Schneider, Pascal Wyss,
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
//  The Initial Developers of the Original Code are Daniel R. Schneider, Pascal Wyss.
//  Portions created by Daniel R. Schneider, Pascal Wyss
//  are Copyright (c) 2010-2016 Daniel R. Schneider, Pascal Wyss, 
//  ETH Zurich, Educational Development and Technology (LET), 
//  based on the original idea of Safe Exam Browser
//  by Stefan Schneider, University of Giessen. All Rights Reserved.
//
//  Contributor(s): ______________________________________.
//

namespace SebWindowsClient
{

	public class Dialog
    {
        public static string ShowPasswordDialogForm(string title, string passwordRequestText)
        {
            // If we are running in SebWindowsClient we need to activate it before showing the password dialog
            if (SEBClientInfo.SebWindowsClientForm != null) SebWindowsClientMain.SEBToForeground();

            return SebPasswordDialogForm.ShowPasswordDialogForm(title, passwordRequestText);
        }

        public static string ShowFileDialogForExecutable(string filename, string originalFilename)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                FileName = filename,
                Filter = filename + " | " + filename,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "exe",
                Title = SEBUIStrings.locatePermittedApplication
            };

            DialogResult fileDialogResult = openFileDialog.ShowDialog();

            // If the user clicked "Cancel", do nothing
            // If the user clicked "OK"    , use the third party applications file name and path as the permitted process
            if (fileDialogResult.Equals(DialogResult.OK))
            {
				var filePath = openFileDialog.FileName;
				var executable = Path.GetFileName(filePath);
				var hasSameName = executable.Equals(filename, StringComparison.InvariantCultureIgnoreCase);
				var hasNoOrSameOriginalName = String.IsNullOrWhiteSpace(originalFilename) || MatchesOriginalFileName(executable, filePath);

				if (hasSameName && hasNoOrSameOriginalName)
				{
					return filePath;
				}
			}

            return null;
        }

		private static bool MatchesOriginalFileName(string executableName, string executablePath)
		{
			try
			{
				var executableInfo = FileVersionInfo.GetVersionInfo(executablePath);

				return executableName.Equals(executableInfo.OriginalFilename, StringComparison.InvariantCultureIgnoreCase);
			}
			catch
			{
			}

			return false;
		}
	}
}

