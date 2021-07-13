using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SocketSampleClient01
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// ホスト名
        /// </summary>
        public string TargetHostName
        {
            get => _targetHostName;
            set
            {
                _targetHostName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ポート番号
        /// </summary>
        public int TargetPortNum
        {
            get => _targetPortNum;
            set
            {
                _targetPortNum = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 送信テキスト
        /// </summary>
        public string SendCommandText
        {
            get => _sendCommandText;
            set
            {
                _sendCommandText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 受信テキスト
        /// </summary>
        public string RecieveText
        {
            get => _recieveText;
            set
            {
                _recieveText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// TcpClientオブジェクト
        /// </summary>
        private TcpClient _tcpClient = null;

        /// <summary>
        /// ホスト名
        /// </summary>
        private string _targetHostName = string.Empty;

        /// <summary>
        /// ポート番号
        /// </summary>
        private int _targetPortNum = 8001;

        /// <summary>
        /// 送信テキスト
        /// </summary>
        private string _sendCommandText = string.Empty;

        /// <summary>
        /// 受信テキスト
        /// </summary>
        private string _recieveText = string.Empty;

        /// <summary>
        /// プロパティ変更通知イベント
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            TargetHostName = GetLocalIPAddress();
        }

        /// <summary>
        /// ローカルIPアドレス取得
        /// </summary>
        /// <returns>ローカルIPアドレス</returns>
        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        /// <summary>
        /// TCP接続
        /// </summary>
        /// <param name="hostName">ホスト名</param>
        /// <param name="portNum">ポート番号</param>
        /// <returns>接続可否</returns>
        private bool OpenTcpClient(string hostName, int portNum)
        {
            try
            {
                _tcpClient = new TcpClient(hostName, portNum);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
        }

        /// <summary>
        /// TCP切断
        /// </summary>
        private void CloseTcpClient()
        {
            _tcpClient?.Close();
        }

        /// <summary>
        /// メッセージ送信
        /// </summary>
        /// <param name="sendText">送信テキスト</param>
        /// <returns>受信テキスト</returns>
        private string SendMessage(string sendText)
        {
            string recieveMsg = string.Empty;

            using (NetworkStream ns = _tcpClient.GetStream())
            {
                ns.ReadTimeout = 1000;
                ns.WriteTimeout = 1000;

                SendTcp(ns, sendText);
                recieveMsg = RecieveTcp(ns);

                ns.Close();
            }

            return recieveMsg;
        }

        /// <summary>
        /// TCP送信
        /// </summary>
        /// <param name="ns">NetworkStream</param>
        /// <param name="sendText">送信テキスト</param>
        private void SendTcp(NetworkStream ns, string sendText)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            byte[] sendBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(sendText + '\n');
            ns.Write(sendBytes, 0, sendBytes.Length);
        }

        /// <summary>
        /// TCP受信
        /// </summary>
        /// <param name="ns">NetworkStream</param>
        /// <returns>受信テキスト</returns>
        private string RecieveTcp(NetworkStream ns)
        {
            string recieveMsg = string.Empty;

            using (MemoryStream ms = new MemoryStream())
            {
                byte[] resBytes = new byte[256];
                int resSize = 0;

                do
                {
                    resSize = ns.Read(resBytes, 0, resBytes.Length);

                    if(resSize == 0)
                    {
                        MessageBox.Show("サーバーが切断しました。");
                        break;
                    }

                    ms.Write(resBytes, 0, resSize);

                } while (ns.DataAvailable || resBytes[resSize - 1] != '\n');

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                recieveMsg = Encoding.GetEncoding("Shift_JIS").GetString(ms.GetBuffer());
            }

            return recieveMsg;
        }

        /// <summary>
        /// Ping送信
        /// </summary>
        /// <param name="hostName">ホスト名</param>
        /// <returns>接続可否</returns>
        private bool SendPing(string hostName)
        {
            Ping pingSender = new Ping();
            PingReply pingReply = pingSender.Send(hostName, 50);
            return (pingReply.Status == IPStatus.Success);
        }

        /// <summary>
        /// 確認ボタン押下イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCheckIP_Click(object sender, RoutedEventArgs e)
        {
            string message = SendPing(TargetHostName) ? "接続可能" : "接続不可";
            MessageBox.Show(message);
        }

        /// <summary>
        /// 送信ボタン押下イベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSendCommand_Click(object sender, RoutedEventArgs e)
        {
            if(OpenTcpClient(TargetHostName, TargetPortNum))
            {
                RecieveText = SendMessage(SendCommandText);
            }

            CloseTcpClient();
        }

        /// <summary>
        /// プロパティ変更通知イベントハンドラ
        /// </summary>
        /// <param name="propertyName"></param>
        private void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
