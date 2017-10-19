using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using Fleck;
using Newtonsoft.Json;
using SebWindowsClient.ConfigurationUtils;
using SebWindowsClient.DiagnosticsUtils;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SebWindowsClient.AdditionalResourcesUtils;

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
                        Logger.AddInformation("Port Occupied, maybe the server is already running");
                        return true;
                    }
                }

                return false;
            }
        }

        public static bool HasBeenReconfiguredByMessage = false;

        public static event EventHandler OnXulRunnerCloseRequested;
        public static event EventHandler OnXulRunnerQuitLinkClicked;
        public static event EventHandler OnXulRunnerTextFocus;
        public static event EventHandler OnXulRunnerTextBlur;
        public static event BrowserEventHandler OnXulRunnerFullscreenchanged;

        public delegate void BrowserEventHandler(dynamic opts);

        private static IWebSocketConnection XULRunner;

        private static int port = 8706;
        private static WebSocketServer server;

        private static List<SEBXULMessage> messageQueue = new List<SEBXULMessage>();

        private static IAdditionalResourceHandler additionalResourceHandler = new AdditionalResourceHandler();

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
            // Check if we're running in exam mode already, if yes, then refuse to load a .seb file
            if (SEBClientInfo.examMode)
            {
                Logger.AddInformation("Reconfiguring SEB using the downloaded Config File data is not allowed because it is already running in exam mode, sending command ReconfigureAborted to browser");
                SEBXULRunnerWebSocketServer.SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.ReconfigureAborted));

                if (SEBClientInfo.SebWindowsClientForm != null) SebWindowsClientMain.SEBToForeground();
                SEBMessageBox.Show(SEBUIStrings.loadingSettingsNotAllowed, SEBUIStrings.loadingSettingsNotAllowedReason, MessageBoxIcon.Error, MessageBoxButtons.OK);
                SebWindowsClientMain.LoadingSebFile(false);
                return;
            }

            HasBeenReconfiguredByMessage = true;
            Logger.AddInformation("Received downloaded Config File data, " + obj.Length + " bytes. Sending command SebFileTransfer to browser");
            SEBXULRunnerWebSocketServer.SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.SebFileTransfer, true));
            if(SEBClientInfo.SebWindowsClientForm.ReconfigureWithSettings(obj))
            {
                Logger.AddInformation("SEB was successfully reconfigured using the downloaded Config File data");
                // Convert new URL Filter rules to XUL seb2 rules
                // and add Start URL to allowed rules
                SEBURLFilter urlFilter = new SEBURLFilter();
                urlFilter.UpdateFilterRules();

                // Create JSON object with XULRunner parameters to pass to firefox.exe as base64 string
                var xulRunnerSettings = DeepClone(SEBSettings.settingsCurrent);
                string XULRunnerParameters = SEBXulRunnerSettings.XULRunnerConfigDictionarySerialize(xulRunnerSettings);

                SEBXULRunnerWebSocketServer.SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.Reconfigure, new { configBase64 = XULRunnerParameters }));
            }
            else
            {
                Logger.AddInformation("Reconfiguring SEB using the downloaded Config File data failed, sending command ReconfigureAborted to browser");
                SEBXULRunnerWebSocketServer.SendMessage(new SEBXULMessage(SEBXULMessage.SEBXULHandler.ReconfigureAborted));
            }
            HasBeenReconfiguredByMessage = false;
        }

        private static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
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

            try
            {
                var sebxulMessage = JsonConvert.DeserializeObject<SEBXULMessage>(message);
                switch (sebxulMessage.Handler)
                {
                    case SEBXULMessage.SEBXULHandler.AdditionalRessourceTriggered:
                        additionalResourceHandler.OpenAdditionalResourceById(sebxulMessage.Opts["Id"].ToString());
                        break;
                    case SEBXULMessage.SEBXULHandler.FullScreenChanged:
                        if (OnXulRunnerFullscreenchanged != null)
                        {
                            OnXulRunnerFullscreenchanged(sebxulMessage.Opts);
                        }
                        break;
                    case SEBXULMessage.SEBXULHandler.ReconfigureSuccess:
                        SEBClientInfo.SebWindowsClientForm.ClosePreviousMainWindow();
                        break;
                }
            }
            //Fallback to old message format
            catch (Exception)
            {
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
