using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.ComponentModel;

namespace lab2 {
    /// <summary>
    /// Interaction logic for ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window {
        
        static TcpListener ServerListener;
        Thread ServerListenThread;

        List<TcpClient> AllClients = new List<TcpClient>();
        //List<NetworkStream> AllStreams = new List<NetworkStream>();
        int ServerPort {
            get {
                return Int32.Parse(TextBox_Port.Text);
            }
        }
        string ServerIp {
            get {
                return TextBox_Ip.Text;
            }
        }
        public ServerWindow() {
            InitializeComponent();
        }
        bool ValidateServerSettings() {
            try {
                _ = ServerPort; // if port parse correct - continue block

                if (ServerPort < 0 || ServerPort > 65535) { return false; } // validate port in range
                if (!IPAddress.TryParse(ServerIp, out _)) { return false; } // validate correct ip
                return true;
            } catch {
                return false;
            }
        }

        void ChangeServerGUIStyleToRunning() {
            if (!ValidateServerSettings()) {
                MessageBox.Show("Incorrect server settings");
                return;
            }
            // switching server status
            Btn_StartServer.IsEnabled = false;
            Btn_StopServer.IsEnabled = true;

            TextBox_Port.IsReadOnly = true;
            TextBox_Ip.IsReadOnly = true;
            Label_ServerStatus.Content = "Running";
            Title = $"Running at {ServerIp}:{ServerPort}";
        }

        void ChangeServerGUIStyleToStopped() {
            Btn_StartServer.IsEnabled = true;
            Btn_StopServer.IsEnabled = false;

            TextBox_Port.IsReadOnly = false;
            TextBox_Ip.IsReadOnly = false;
            Label_ServerStatus.Content = "Offline";
            Title = "Server";
        }

        void RunServer(string ip, int port) {
            ServerListener = new TcpListener(IPAddress.Parse(ip), port);
            ServerListener.Start();
            ServerListenThread = new Thread(() => listen(ref ServerListener));
            ServerListenThread.Start();
        }


        void StopServer(ref TcpListener listener) {
            ServerListener.Server.Close();
            ServerListenThread.Abort();
            listener.Stop();
        }

        void listen(ref TcpListener listener) {
            while (true) {

                TcpClient client = listener.AcceptTcpClient();
                Dispatcher.BeginInvoke(new Action(() => AllClients.Add(client)));

                string clientIPAddress = IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()).ToString();
                Dispatcher.BeginInvoke(new Action(() => ListBox_ServerLog.Items.Add($"New clinet connected; ip: {clientIPAddress}")));

                Thread newClientThread = new Thread(() => ServeClient(client));
                newClientThread.Start();

            }
        }

        void ServeClient(TcpClient client) {
           

            NetworkStream stream = null;
            try {
                
                stream = client.GetStream();
                byte[] data = new byte[64];
                while (true) {
                    StringBuilder strbuilder = new StringBuilder();
                    int bytes = 0;
                    do {
                        bytes = stream.Read(data, 0, data.Length);
                        strbuilder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    } while (stream.DataAvailable);

                    string receivedClientStringData = strbuilder.ToString();
                    
                    Dispatcher.BeginInvoke(new Action(() => ListBox_ServerLog.Items.Add(receivedClientStringData)));
                    

                    string recievedMessage;
                    string recievedUsername;
                    bool needReverseMessage;
                    bool isSendToGlobalChat;
                    string recievedDataType;
                    try { // expecting json with this fields:
                        dynamic parsedRecievedJson = JObject.Parse(receivedClientStringData);
                        recievedMessage = parsedRecievedJson.message;
                        recievedUsername = parsedRecievedJson.username;
                        needReverseMessage = parsedRecievedJson.isReverseMessage;
                        isSendToGlobalChat = parsedRecievedJson.isToGlobalChat;
                        recievedDataType = parsedRecievedJson.dataType;
                    } catch (Exception ex) {
                        Dispatcher.BeginInvoke(new Action(() => ListBox_ServerLog.Items.Add($"Error parsing client json: {ex.Message}")));
                        continue;
                    }
                    if (recievedDataType != "message") {
                        Dispatcher.BeginInvoke(new Action(() => ListBox_ServerLog.Items.Add($"Recieved uncnoun data type")));
                        continue;
                    }
                    if (needReverseMessage) {
                        recievedMessage = new string(recievedMessage.Reverse().ToArray());
                    }

                    string responceMessage = $"{recievedUsername} says: {recievedMessage}";
                    byte[] encodedResponceMessage = new byte[64];
                    encodedResponceMessage = Encoding.Unicode.GetBytes(responceMessage);
                    if (isSendToGlobalChat) {
                        SendDataToAllClients(encodedResponceMessage);
                    } else {
                        SendDataToConnectedClient(client, encodedResponceMessage);
                    }

                    
                }
            } catch (Exception ex) {
                Dispatcher.BeginInvoke(new Action(() => ListBox_ServerLog.Items.Add(ex.Message)));
            } finally {
                if (true) { stream.Close(); }
                if (true) { client.Close(); }
            }
        }

        void SendDataToConnectedClient(TcpClient client, byte[] jsonData) {
            NetworkStream stream = client.GetStream();
            if (client.Connected) {
                stream.Write(jsonData, 0, jsonData.Length);
                stream.Flush();
            }
        }

        void SendDataToAllClients(byte[] jsonData) {
            
            foreach (TcpClient client in AllClients) {
                if (client.Connected) {
                    SendDataToConnectedClient(client, jsonData);
                }
            }
            
        }
        


        private void Btn_StartServer_Click(object sender, RoutedEventArgs e) {
            ChangeServerGUIStyleToRunning();
            RunServer(ServerIp, ServerPort);
        }

        private void Btn_StopServer_Click(object sender, RoutedEventArgs e) {
            ChangeServerGUIStyleToStopped();
            StopServer(ref ServerListener);
        }

        
    }
}
