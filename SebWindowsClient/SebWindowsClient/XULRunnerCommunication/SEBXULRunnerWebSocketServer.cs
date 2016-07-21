using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using Fleck;
using Newtonsoft.Json;
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
                        Logger.AddInformation("Server already running!");
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

        private static List<SEBXULMessage> messageQueue = new List<SEBXULMessage>();

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
                    socket.OnBinary = OnClientMessageBinary;
                });
                Logger.AddInformation("Started WebSocketServer on " + ServerAddress);
                Started = true;
            }
            catch (Exception ex)
            {
                Logger.AddError("Unable to start WebSocketsServer for communication with XULRunner", null, ex);
            }
        }

        private static void OnClientMessageBinary(byte[] obj)
        {
            SEBClientInfo.SebWindowsClientForm.ReconfigureWithSettings(obj);
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
            foreach (var sebxulMessage in messageQueue)
            {
                SendMessage(sebxulMessage);
            }
        }

        public static void SendMessage(SEBXULMessage message)
        {
            try
            {
                if (XULRunner != null)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(message));
                    Logger.AddInformation("WebSocket: Send message: " + JsonConvert.SerializeObject(message));
                    XULRunner.Send(JsonConvert.SerializeObject(message));
                }
                else
                {
                    Logger.AddInformation("WebSocket: Added message to queue: " + JsonConvert.SerializeObject(message));
                    messageQueue.Add(message);
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

        [Obsolete("Window gets resized by SEB not seb")]
        public static void SendDisplaySettingsChanged()
        {
            SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.DisplaySettingsChanged));
        }

        public static void SendKeyboardShown()
        {
            SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.KeyboardShown));
        }

        public static void SendAllowCloseToXulRunner()
        {
            SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.Close));
        }

        public static void SendRestartExam()
        {
            SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.RestartExam));
        }

        public static void SendReloadPage()
        {
            SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.Reload));
        }
    }
}
