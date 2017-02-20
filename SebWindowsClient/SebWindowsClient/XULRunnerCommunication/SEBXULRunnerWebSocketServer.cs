using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using Fleck;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DiagnosticsUtils;

namespace SebWindowsClient.XULRunnerCommunication
{
    /// <summary>
    /// WebSocket Server to communicate with the XULRunner
    /// </summary>
    public class SEBXULRunnerWebSocketServer
    {
        public static bool Started = false;

        /// <summary>
        /// The URL to connect to
        /// </summary>
        public static string ServerAddress
        {
            get
            {
                return String.Format("ws://localhost:{0}",port);
            }
        }

        public static bool IsRunning
        {
            get
            {
                if (server != null)
                    return true;

                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

                foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                {
                    if (tcpi.LocalEndPoint.Port == port && tcpi.State != TcpState.TimeWait)
                    {
                        Logger.AddInformation("Server already running or port occupied!");
                        return true;
                    }
                }

                return false;
            }
        }

        public static event EventHandler OnXulRunnerCloseRequested;
        public static event EventHandler OnXulRunnerQuitLinkClicked;
        public static event EventHandler OnXulRunnerTextFocus;
        public static event EventHandler OnXulRunnerTextBlur;

        private static IWebSocketConnection XULRunner;

        private static int port = 8706;
        private static WebSocketServer server;

        /// <summary>
        /// Start the server if not already running
        /// </summary>
        public static void StartServer()
        {
            if (IsRunning && Started)
                return;

            if (IsRunning)
            {
                for (int i = 0; i < 60; i++)
                {
                    if (!IsRunning)
                        break;

                    Thread.Sleep(1000);
                }
                if (IsRunning)
                    SEBMessageBox.Show(SEBUIStrings.alertWebSocketPortBlocked, SEBUIStrings.alertWebSocketPortBlockedMessage, MessageBoxIcon.Error, MessageBoxButtons.OK);
                    //MessageBox.Show(SEBUIStrings.alertWebSocketPortBlocked);
            }

            try
            {
                Logger.AddInformation("Starting WebSocketServer on " + ServerAddress);
                server = new WebSocketServer(ServerAddress);
                FleckLog.Level = LogLevel.Debug;
                server.Start(socket =>
                {
                    socket.OnOpen = () => OnClientConnected(socket);
                    socket.OnClose = OnClientDisconnected;
                    socket.OnMessage = OnClientMessage;
                });
                Logger.AddInformation("Started WebSocketServer on " + ServerAddress);
                Started = true;
            }
            catch (Exception ex)
            {
                Logger.AddError("Unable to start WebSocketsServer for communication with XULRunner", null, ex);
            }
        }

        private static void OnClientDisconnected()
        {
            Logger.AddInformation("WebSocket: Client disconnected");
            XULRunner = null;
        }

        private static void OnClientConnected(IWebSocketConnection socket)
        {
            Logger.AddInformation("WebSocket: Client Connectedon port:" + socket.ConnectionInfo.ClientPort);
            XULRunner = socket;
        }

        public static void SendAllowCloseToXulRunner()
        {
            try
            {
                if (XULRunner != null)
                {
                    Console.WriteLine("SEB.Close sent");
                    Logger.AddInformation("WebSocket: Send message: SEB.close");
                    XULRunner.Send("SEB.close");
                }
            }
            catch (Exception)
            {
            }
        }

        public static void SendRestartExam()
        {
            try
            {
                if (XULRunner != null && 
                    (!string.IsNullOrEmpty((String)SEBClientInfo.getSebSetting(SEBSettings.KeyRestartExamURL)[SEBSettings.KeyRestartExamURL])
                    || (bool)SEBClientInfo.getSebSetting(SEBSettings.KeyRestartExamUseStartURL)[SEBSettings.KeyRestartExamUseStartURL]))
                {
                    Console.WriteLine("SEB.Restart Exam sent");
                    Logger.AddInformation("WebSocket: Send message: SEB.restartExam");
                    XULRunner.Send("SEB.restartExam");
                }
            }
            catch (Exception)
            {
            }
        }

        public static void SendReloadPage()
        {
            try
            {
                if (XULRunner != null)
                {
                    Console.WriteLine("SEB.Reload Sent");
                    Logger.AddInformation("WebSocket: Send message: SEB.reload");
                    XULRunner.Send("SEB.reload");
                }
            }
            catch (Exception)
            {
            }
        }

        private static void OnClientMessage(string message)
        {
            Console.WriteLine("RECV: " + message);
            Logger.AddInformation("WebSocket: Received message: " + message);
            switch (message)
            {
                case "seb.beforeclose.manual":
                    if (OnXulRunnerCloseRequested != null)
                        OnXulRunnerCloseRequested(null, EventArgs.Empty);
                    break;
                case "seb.beforeclose.quiturl":
                    if (OnXulRunnerQuitLinkClicked != null)
                        OnXulRunnerQuitLinkClicked(null, EventArgs.Empty);
                    break;
                case "seb.input.focus":
                    if (OnXulRunnerTextFocus != null)
                        OnXulRunnerTextFocus(null, EventArgs.Empty);
                    break;
                case "seb.input.blur":
                    if (OnXulRunnerTextBlur != null)
                        OnXulRunnerTextBlur(null, EventArgs.Empty);
                    break;
            }
        }

        public static void SendDisplaySettingsChanged()
        {
            try
            {
                if (XULRunner != null)
                {
                    Console.WriteLine("SEB.ChangedDisplaySettingsChanged");
                    Logger.AddInformation("WebSocket: Send message: SEB.displaySettingsChanged");
                    XULRunner.Send("SEB.displaySettingsChanged");
                }
            }
            catch (Exception)
            {
            }
        }

        public static void SendKeyboardShown()
        {
            try
            {
                if (XULRunner != null)
                {
                    Console.WriteLine("SEB.keyboardShown");
                    Logger.AddInformation("WebSocket: Send message: SEB.keyboardShown");
                    XULRunner.Send("SEB.keyboardShown");
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
