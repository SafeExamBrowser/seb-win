using System;
using System.Data;
using System.Management;

namespace SebWindowsClient.ProcessUtils
{
    /// <summary>
    /// ProcessInfo class.
    /// </summary>
    public class ProcessInfo
    {
        // defenition of the delegates
        public delegate void StartedEventHandler(object sender, EventArgs e);
        public delegate void TerminatedEventHandler(object sender, EventArgs e);

        // events to subscribe
        public StartedEventHandler Started = null;
        public TerminatedEventHandler Terminated = null;

        public string ProcessName
        { get; set; }

        // WMI event watcher
        private ManagementEventWatcher watcher;

        // The constructor uses the application name like notepad.exe
        // And it starts the watcher
        public ProcessInfo(string appName)
        {
            this.ProcessName = appName;

            // querry every 2 seconds
            string pol = "2";

            string queryString =
                "SELECT *" +
                "  FROM __InstanceOperationEvent " +
                "WITHIN  " + pol +
                " WHERE TargetInstance ISA 'Win32_Process' " +
                "   AND TargetInstance.Name = '" + appName + "'";

            // You could replace the dot by a machine name to watch to that machine
            string scope = @"\\.\root\CIMV2";

            // create the watcher and start to listen
            watcher = new ManagementEventWatcher(scope, queryString);
            watcher.EventArrived += new EventArrivedEventHandler(this.OnEventArrived);
            watcher.Start();
        }
        public void Dispose()
        {
            watcher.Stop();
            watcher.Dispose();
        }
        public static DataTable RunningProcesses()
        {
            /* One way of constructing a query
            string className = "Win32_Process";
            string condition = "";
            string[] selectedProperties = new string[] {"Name", "ProcessId", "Caption", "ExecutablePath"};
            SelectQuery query = new SelectQuery(className, condition, selectedProperties);
            */

            // The second way of constructing a query
            string queryString =
                "SELECT Name, ProcessId, Caption, ExecutablePath" +
                "  FROM Win32_Process";

            SelectQuery query = new SelectQuery(queryString);
            ManagementScope scope = new System.Management.ManagementScope(@"\\.\root\CIMV2");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection processes = searcher.Get();

            DataTable result = new DataTable();
            result.Columns.Add("Name", Type.GetType("System.String"));
            result.Columns.Add("ProcessId", Type.GetType("System.Int32"));
            result.Columns.Add("Caption", Type.GetType("System.String"));
            result.Columns.Add("Path", Type.GetType("System.String"));

            foreach (ManagementObject mo in processes)
            {
                DataRow row = result.NewRow();
                row["Name"] = mo["Name"].ToString();
                row["ProcessId"] = Convert.ToInt32(mo["ProcessId"]);
                if (mo["Caption"] != null)
                    row["Caption"] = mo["Caption"].ToString();
                if (mo["ExecutablePath"] != null)
                    row["Path"] = mo["ExecutablePath"].ToString();
                result.Rows.Add(row);
            }
            return result;
        }
        private void OnEventArrived(object sender, System.Management.EventArrivedEventArgs e)
        {
            try
            {
                string eventName = e.NewEvent.ClassPath.ClassName;

                if (eventName.CompareTo("__InstanceCreationEvent") == 0)
                {
                    // Started
                    if (Started != null)
                        Started(this, e);
                }
                else if (eventName.CompareTo("__InstanceDeletionEvent") == 0)
                {
                    // Terminated
                    if (Terminated != null)
                        Terminated(this, e);

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

    }
}
