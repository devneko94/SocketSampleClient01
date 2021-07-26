using System;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SocketSampleClient01
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region プロパティ
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
        public string OutputText
        {
            get => _outputText;
            set
            {
                _outputText = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region イベント
        /// <summary>
        /// プロパティ変更通知イベント
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region フィールド
        /// <summary>
        /// TcpClientオブジェクト
        /// </summary>
        private MyTcpClient _tcpClient = null;

        /// <summary>
        /// ホスト名
        /// </summary>
        private string _targetHostName = "127.0.0.1";

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
        private string _outputText = string.Empty;
        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        #endregion

        #region プライベートメソッド
        /// <summary>
        /// プロパティ変更通知
        /// </summary>
        /// <param name="propertyName"></param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Main()
        {
            // クライアントインスタンス作成
            _tcpClient = new MyTcpClient();

            // 接続試行
            if (!_tcpClient.TryConnection(TargetHostName, TargetPortNum, 1000)) { return; }

            // テキスト送信
            byte[] data = Encoding.GetEncoding("Shift_JIS").GetBytes(SendCommandText);
            _tcpClient.Write(data);

            // テキスト受信
            byte[] buffer = _tcpClient.Read((buf) => true, 15000);
            string recieveText = Encoding.GetEncoding("Shift_JIS").GetString(buffer);
            OutputText += recieveText + Environment.NewLine;

            // 接続解除
            _tcpClient.Close();
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
        /// 確認ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCheckIP_Click(object sender, RoutedEventArgs e)
        {
            string message = SendPing(TargetHostName) ? "接続可能" : "接続不可";
            MessageBox.Show(message);
        }

        /// <summary>
        /// 送信ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSendCommand_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => { Main(); });
        }
        #endregion

        #region 内部クラス
        private class MyTcpClient: TcpClient
        {
            #region コンストラクタ
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public MyTcpClient() : base() { }
            #endregion

            #region パブリックメソッド
            /// <summary>
            /// 接続試行
            /// </summary>
            /// <param name="addr">ホスト名</param>
            /// <param name="port">ポート番号</param>
            /// <param name="ms">タイムアウト時間(ms)</param>
            /// <returns>接続成否</returns>
            public bool TryConnection(string addr, int port, int ms)
            {
                // 非同期接続
                IAsyncResult ar = this.BeginConnect(addr, port, null, null);

                // 接続待機
                WaitHandle handle = ar.AsyncWaitHandle;

                try
                {
                    if (!handle.WaitOne(ms, false))
                    {
                        // タイムアウト
                        this.Close();
                        return false;
                    }
                    else
                    {
                        // 接続成功
                        this.EndConnect(ar);
                        return true;
                    }
                }
                finally
                {
                    handle.Close();
                }
            }

            /// <summary>
            /// データ送信
            /// </summary>
            /// <param name="data">送信データ</param>
            public void Write(byte[] data)
            {
                NetworkStream ns = this.GetStream();
                ns.Write(data, 0, data.Length);
            }

            /// <summary>
            /// データ受信
            /// </summary>
            /// <param name="isComplete">受信完了判断処理</param>
            /// <param name="ms">タイムアウト時間(ms)</param>
            /// <returns>受信データ</returns>
            public byte[] Read(Func<byte[], bool> isComplete, int ms)
            {
                try
                {
                    byte[] buffer = new byte[256];
                    NetworkStream ns = this.GetStream();
                    ns.ReadTimeout = ms;

                    while (true)
                    {
                        int size = ns.Read(buffer, 0, buffer.Length);

                        if (size == 0)
                        {
                            return Array.Empty<byte>();
                        }

                        if (isComplete(buffer))
                        {
                            break;
                        }
                    }
                    return buffer;
                }
                catch
                {
                    return Array.Empty<byte>();
                }
            }
            #endregion
        }
        #endregion
    }
}
