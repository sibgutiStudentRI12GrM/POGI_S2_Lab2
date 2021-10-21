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
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window {
        TcpClient Client = null;
        NetworkStream Stream = null;
        Thread ListenThread = null;
        bool ShowErrors = false; // must be false in realise

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

        string ClientUsername {
            get {
                return TextBox_Username.Text;
            }
        }

        string TypedMessage {
            get {
                return TextBox_Message.Text;
            }
        }

        bool IsTypedMessageToGlobalChat {
            get {
                return Convert.ToBoolean(CheckBox_MessageToGlobalChat.IsChecked);
            }
        }

        bool IsReverseTypedMessage {
            get {
                return Convert.ToBoolean(CheckBox_ReverseMessage.IsChecked);
            }
        }


        public ClientWindow() {
            InitializeComponent();
        }

        bool VerifyClientSettings() {
            try {
                _ = ServerPort; // if port parse correct - continue block

                if (ServerPort < 0 || ServerPort > 65535) { return false; } // validate port in range
                if (!IPAddress.TryParse(ServerIp, out _)) { return false; } // validate correct ip

                // username validation
                Regex regex = new Regex(@"^(?=[a-zA-Z])[-\w.]{0,23}([a-zA-Z\d]|(?<![-.])_)$");
                if (!regex.IsMatch(TextBox_Username.Text)) { return false; }
                return true;
            } catch {
                return false;
            }
        }

        void ChangeWindowStyleToConnected() {
            Btn_Connect.IsEnabled = false;
            Btn_Disconnect.IsEnabled = true;
            Btn_SendMessage.IsEnabled = true;

            TextBox_Ip.IsReadOnly = true;
            TextBox_Port.IsReadOnly = true;
            TextBox_Username.IsReadOnly = true;

            Title = $"Connection: {ClientUsername}@{ServerIp}:{ServerPort}";
        }

        void ChangeWindowStyleToDisconnected() {
            Btn_Connect.IsEnabled = true;
            Btn_Disconnect.IsEnabled = false;
            Btn_SendMessage.IsEnabled = false;

            TextBox_Ip.IsReadOnly = false;
            TextBox_Port.IsReadOnly = false;
            TextBox_Username.IsReadOnly = false;

            Title = "Client";

        }


        bool ConnectToServer(string ip, int port) {
            try {
                Client = new TcpClient(ip, port);
                Stream = Client.GetStream();
                ListenThread = new Thread(() => ListenToServer());
                ListenThread.Start();
                return true;
            } catch (Exception ex) {
                Dispatcher.BeginInvoke(new Action(() => ListBox_ClientLog.Items.Add(ex.Message)));
                return false;
            }

        }

        void DisconnectFromServer() {
            ListenThread.Abort();
            ListenThread = null;
            

        }

        void ListenToServer() {
            try {
                while (true) {
                    byte[] data = new byte[64];
                    StringBuilder stringbuilder = new StringBuilder();
                    int bytes = 0;

                    do {
                        bytes = Stream.Read(data, 0, data.Length);
                        stringbuilder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    } while (Stream.DataAvailable);

                    string recievedServerMessage = stringbuilder.ToString();
                    Dispatcher.BeginInvoke(new Action(() => ListBox_ClientLog.Items.Add(recievedServerMessage)));
                }
            } catch (Exception ex) {
                if (ShowErrors) { Dispatcher.BeginInvoke(new Action(() => ListBox_ClientLog.Items.Add(ex.Message))); }
            } finally {
                
            }
        }

        void SendJsonToServer(string serializedJson) {
            byte[] data = Encoding.Unicode.GetBytes(serializedJson);
            Stream.Write(data, 0, data.Length);
        }

        private void Btn_Connect_Click(object sender, RoutedEventArgs e) {
            if (!VerifyClientSettings()) { MessageBox.Show("Check client settings"); return; }
            if (ConnectToServer(ServerIp, ServerPort)) { ChangeWindowStyleToConnected(); }
            



        }

        private void Btn_Disconnect_Click(object sender, RoutedEventArgs e) {
            ChangeWindowStyleToDisconnected();
            DisconnectFromServer();
            
            
        }
        
        private void Btn_SendMessage_Click(object sender, RoutedEventArgs e) {
            
            // Creating JS object with gui data inside
            dynamic messageJsObj = new JObject();
            messageJsObj.dataType = "message";
            messageJsObj.username = ClientUsername;
            messageJsObj.message = TypedMessage;
            messageJsObj.isToGlobalChat = IsTypedMessageToGlobalChat;
            messageJsObj.isReverseMessage = IsReverseTypedMessage;
            // serializing message with metadata to json string:
            string jsonSerializedMessage = messageJsObj.ToString(Formatting.None); // Formatting is for minimalizing json string

            SendJsonToServer(jsonSerializedMessage);
            TextBox_Message.Text = "";
        }

        
    }
}
