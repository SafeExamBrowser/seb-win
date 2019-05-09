// -------------------------------------------------------------
//     Viktor tomas
//     BFH-TI, http://www.ti.bfh.ch
//     Biel, 2012
// -------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.Management;
using SebWindowsClient.DiagnosticsUtils;

namespace SebWindowsClient.ProcessUtils
{
	public class SEBNotAllowedProcessController
    {
        /// <summary>
        /// Check if a process is running
        /// </summary>
        /// <param name="processname">the processname</param>
        /// <returns>true if the process runns otherwise false</returns>
        public static bool CheckIfAProcessIsRunning(string processname)
        {
            return System.Diagnostics.Process.GetProcessesByName(processname).Length > 0;
        }

        /// <summary>
        /// Gets process owner.
        /// </summary>
        /// <returns></returns>
        public static string getLocalProcessOwner(int pid)
        {
            string ProcessOwner = "";
            ObjectQuery x = new ObjectQuery("Select * From Win32_Process where Handle='" + pid + "'");
            ManagementObjectSearcher mos = new ManagementObjectSearcher(x);
            foreach (ManagementObject mo in mos.Get())
            {
                string[] s = new string[2];
                mo.InvokeMethod("GetOwner", (object[])s);
                ProcessOwner = s[0].ToString();
                break;
            }

            return ProcessOwner;
        }


        /// <summary>
        /// Closes process by process name.
        /// </summary>
        /// <returns></returns>
        public static bool CloseProcess(Process processToClose)
        {
            try
            {
                if (processToClose == null) // && !processToClose.HasExited)
                {
                    return true;
                }
                else
                {
                    string name = "processHasExitedTrue";
                    name = processToClose.ProcessName;
                    Logger.AddInformation("Closing " + name);

                    // Try to close process nicely with CloseMainWindow
                    try
                    {
                        if (processToClose.HasExited)
                        {
                            return true;
                        }
                        else
                        {
                            Logger.AddInformation("Process " + name + " hasnt closed yet, try again");
                            //If the process handles the mainWindow
                            if (processToClose != null && !processToClose.HasExited && processToClose.MainWindowHandle != IntPtr.Zero)
                            {
                                // Close process by sending a close message to its main window.
                                Logger.AddError("Send CloseMainWindow to process " + name, null, null);
                                processToClose.CloseMainWindow();
                                // Wait max. 5 seconds till the process exits
                                for (int i = 0; i < 5; i++)
                                {
                                    processToClose.Refresh();
                                    // If process still hasn't exited, we wait another second
                                    if (processToClose != null && !processToClose.HasExited)
                                    {
                                        Logger.AddError("Process " + name + " hasn't exited by closing its main window, wait up to one more second and check again.", null, null);
                                        //Thread.Sleep(1000);
                                        processToClose.WaitForExit(1000);
                                    }
                                    else
                                    {
                                        Logger.AddInformation("Process " + name + " has exited.", null, null);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Logger.AddInformation("Send Kill to process " + name);
                        processToClose.Kill();
                        Logger.AddInformation("Successfully sent Kill to process " + name);
                    }

                    processToClose.Refresh();

                    // Check if process has exited now and otherwise kill it
                    if (processToClose.HasExited)
                    {
                        return true;
                    }
                    else
                    {
                        // If process still hasn't exited, we kill it
                        Logger.AddInformation("Send Kill to process " + name);
                        processToClose.Kill();
                        // Wait max. 10 seconds till the process exits
                        for (int i = 0; i < 10; i++)
                        {
                            processToClose.Refresh();
                            // If process still hasn't exited, we wait another second
                            if (!processToClose.HasExited)
                            {
                                Logger.AddError("Process " + name + " still hasn't exited, wait up to one more second and check again.", null, null);
                                //Thread.Sleep(1000);
                                try
                                {
                                    processToClose.WaitForExit(1000);
                                }
                                catch (Exception ex)
                                {
                                    Logger.AddError("Unable to processToClose.WaitForExit(1000)",null,ex);
                                }
                                        
                            }
                            else
                            {
                                Logger.AddInformation("Process " + name + " has exited.");
                                break;
                            }
                        }
                    }
                    processToClose.Refresh();
                             
                    // If process still hasn't exited or wasn't closed, we log this
                    if (!processToClose.HasExited)
                    {
                        Logger.AddError("Process " + name + " has not exited after killing it and waiting in total 11 seconds!", null, null);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddError("Error when killing process", null, ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets all processes.
        /// </summary>
        /// <returns></returns>
        public static Hashtable GetProcesses()
        {
            Hashtable ht = new Hashtable();
            Process[] processes = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process process in processes)
                ht.Add(Convert.ToInt32(process.Id), process.ProcessName);
            return ht;
        }

        /// <summary>
        /// Kills the process by name.
        /// </summary>
        /// <param name="nameToKill">The process name.</param>
        public static void KillProcessByName(string nameToKill)
        {
            try
            {
                Process[] processes = System.Diagnostics.Process.GetProcesses();
                foreach (System.Diagnostics.Process process in processes)
                    if (process.ProcessName == nameToKill)
                        process.Kill();
            }
            catch (Exception ex)
            {
                Logger.AddError("Error when killing process", null, ex);
            }
        }

        /// <summary>
        /// Kills the process by id.
        /// </summary>
        /// <param name="idToKill">The process Id.</param>
        public static void KillProcessById(int idToKill)
        {
            Process[] processes = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process process in processes)
                if (process.Id == idToKill)
                    process.Kill();
        }


    }
}
