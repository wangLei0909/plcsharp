using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using PLCSharp.VVMs.Connects.ModbusTcp;
using PLCSharp.VVMs.Connects.Socket;
using Prism.Mvvm;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace PLCSharp.VVMs.Connects
{
    public enum CommunicationDataType
    {

        String,
        Bytes,
    }
    /// <summary>
    /// 连接
    /// </summary>
    public class Connect : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        private bool SetWithPrompt<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
                Params.Prompt = "已修改，请保存";
            return SetProperty(ref field, value, propName);
        }

        private string _Name;
        /// <summary>
        /// 连接配置
        /// </summary>
        public string Name
        {
            get => _Name;
            set => SetWithPrompt(ref _Name, value);
        }

        private string _IP_SerialPort;
        /// <summary>
        /// IP_串口端口
        /// </summary>
        public string IP_SerialPort
        {
            get => _IP_SerialPort;
            set => SetWithPrompt(ref _IP_SerialPort, value);
        }

        private int _Port;
        /// <summary>
        /// 端口
        /// </summary>
        public int Port
        {
            get => _Port;
            set => SetWithPrompt(ref _Port, value);
        }

        private ProtocolType _Type;
        /// <summary>
        /// 类型
        /// </summary>
        [JsonIgnore]
        /// <summary>
        /// 协议类型
        /// </summary>
        public ProtocolType Type
        {
            get { return _Type; }
            set
            {
                if (_Type == ProtocolType.Undefined)
                {
                    if (value != ProtocolType.Undefined)
                    {
                        SetProperty(ref _Type, value);

                    }
                }
                else if (_Type != value)
                {
                    MessageBox.Show("选定协议后不可改变！");
                }

            }
        }

        private string _Comment;
        /// <summary>
        /// 备注
        /// </summary>
        public string Comment
        {
            get => _Comment;
            set => SetWithPrompt(ref _Comment, value);
        }

        #region NotMapped

        private bool _LogSwitch;
        /// <summary>
        /// 日志Switch
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 日志开关
        /// </summary>
        public bool LogSwitch
        {
            get { return _LogSwitch; }
            set { SetProperty(ref _LogSwitch, value); }
        }

        private bool _Connected;
        /// <summary>
        /// Connected
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 连接成功
        /// </summary>
        public bool Connected
        {
            get { return _Connected; }
            set { SetProperty(ref _Connected, value); }
        }

        private bool _Successful;
        /// <summary>
        /// Successful
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 读写成功
        /// </summary>
        public bool Successful
        {
            get { return _Successful; }
            set { SetProperty(ref _Successful, value); }
        }


        private bool _Online;
        /// <summary>
        /// Online
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 在线
        /// </summary>
        public bool Online
        {
            get { return _Online; }
            set { SetProperty(ref _Online, value); }
        }

        private string _ReceiveInfo;
        /// <summary>
        /// 接收信息
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 收到的信息
        /// </summary>
        public string ReceiveInfo
        {
            get { return _ReceiveInfo; }
            set { SetProperty(ref _ReceiveInfo, value); }
        }


        private byte[] _ReceiveData;
        /// <summary>
        /// 接收数据
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 收到的数据
        /// </summary>
        public byte[] ReceiveData
        {
            get { return _ReceiveData; }
            set { SetProperty(ref _ReceiveData, value); }
        }
        private string _SendInfo;
        /// <summary>
        /// 发送信息
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 要发送的信息
        /// </summary>
        public string SendInfo
        {
            get { return _SendInfo; }
            set { SetProperty(ref _SendInfo, value); }
        }

        /// <summary>
        /// 序列化Params
        /// </summary>
        [Column("Params")]
        public string SerializedParams
        {
            get => JsonConvert.SerializeObject(Params); // 自动序列化
            set => Params = JsonConvert.DeserializeObject<ConnectParams>(value); // 自动反序列化

        }

        private ObservableCollection<DataItem> _DataItems = [];
        /// <summary>
        /// 数据Items
        /// </summary>
        [NotMapped]
        public ObservableCollection<DataItem> DataItems
        {
            get => _DataItems;
            set => SetProperty(ref _DataItems, value);
        }
        /// <summary>
        /// 序列化数据Items
        /// </summary>
        [Column("DataItems")]
        public string SerializedDataItems
        {
            get => JsonConvert.SerializeObject(DataItems); // 自动序列化
            set => DataItems = value != null ? JsonConvert.DeserializeObject<ObservableCollection<DataItem>>(value) : []; // 自动反序列化

        }

        private bool _PollEnabled = true;
        /// <summary>是否启用轮询</summary>
        [NotMapped]
        public bool PollEnabled
        {
            get => _PollEnabled;
            set => SetProperty(ref _PollEnabled, value);
        }

        private int _PollIntervalMs = 1000;
        /// <summary>轮询间隔（毫秒）</summary>
        [NotMapped]
        public int PollIntervalMs
        {
            get => _PollIntervalMs;
            set
            {
                if (value < 100) value = 100;
                if (value > 60000) value = 60000;
                SetProperty(ref _PollIntervalMs, value);
            }
        }

        private ConnectParams _Params;
        /// <summary>
        /// 参数集合
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 连接配置
        /// </summary>
        public ConnectParams Params
        {
            get
            {
                _Params ??= new ConnectParams();

                return _Params;
            }
            set
            {
                SetProperty(ref _Params, value);
            }
        }

        private ObservableCollection<Connect> _Clients = [];
        /// <summary>
        /// Clients
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 连接 服务器 的 客户端
        /// </summary>
        public ObservableCollection<Connect> Clients
        {
            get { return _Clients; }
            set { SetProperty(ref _Clients, value); }
        }

        /// <summary>
        /// 序列化Clients
        /// </summary>
        [Column("Clients")]
        public string SerializedClients
        {
            get => JsonConvert.SerializeObject(Clients); // 自动序列化
            set => Clients = JsonConvert.DeserializeObject<ObservableCollection<Connect>>(value); // 自动反序列化

        }
        /// <summary>
        /// 打开
        /// </summary>
        public virtual void Open() { }

        /// <summary>
        /// 关闭
        /// </summary>
        public virtual void Close() { }
        /// <summary>
        /// 日志Queue
        /// </summary>
        [NotMapped]
        public ConcurrentQueue<string> LogQueue { get; set; } = [];
        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="log">日志</param>
        public void Log(string log)
        {
            if (LogSwitch)
            {

                LogQueue.Enqueue(log);

                if (LogQueue.Count > 1000)
                    LogQueue.TryDequeue(out _);
                RaisePropertyChanged("LogQueue");
            }
        }
        #endregion

        /// <summary>
        /// HexStringToBytes
        /// </summary>
        /// <param name="hexString">hexString</param>
        /// <param name="bytes">bytes</param>
        /// <param name="format">format</param>
        /// <returns>返回布尔值</returns>
        public static bool HexStringToBytes(string hexString, out List<byte> bytes, out string format)
        {
            format = hexString;
            bytes = [];
            var sendMsg = hexString.Replace(" ", "").Replace("-", "").Replace("\n", "").Replace("\r", "");
            if (sendMsg.Length % 2 != 0)
            {
                sendMsg = "0" + sendMsg;
            }
            sendMsg = sendMsg.ToUpper();

            bool isMatch = Regex.IsMatch(sendMsg, @"^[0-9A-Fa-f]+$");
            if (isMatch)
            {

                StringBuilder sb = new();
                for (int i = 0; i < sendMsg.Length; i++)
                {
                    if (i > 0 && i % 2 == 0)
                        sb.Append(' '); // 每隔两位插入空格
                    sb.Append(sendMsg[i]);
                }
                format = sb.ToString();
                var split = format.Split(" ");


                for (int i = 0; i < split.Length; i++)
                {
                    bytes.Add(byte.Parse(split[i], NumberStyles.HexNumber));
                }
                return true;


            }
            else
            {

                return false;
            }

        }

        /// <summary>
        /// 发送Async
        /// </summary>
        /// <param name="msg">msg</param>
        /// <returns>返回结果</returns>
        public virtual async Task SendAsync(string msg)
        {
            if (Params.DataType == CommunicationDataType.String)
            {
                await SendMsgAsync(msg);

            }
            else
            {
                if (HexStringToBytes(msg, out List<byte> bytes, out string format))
                {
                    SendInfo = format;
                    await SendDataAsync([.. bytes]);
                }
                else
                {
                    Log("请输入正确的十六进制字符串");
                }
            }

        }

        /// <summary>
        /// 发送Async
        /// </summary>
        /// <param name="msg">msg</param>
        /// <param name="clientName">客户端名称</param>
        /// <returns>返回结果</returns>
        public virtual async Task SendAsync(string msg, string clientName)
        {

            if (this is SocketServer server)
            {
                if (Params.DataType == CommunicationDataType.String)
                {
                    await server.SendMsgAsync(msg, clientName);

                }
                else
                {
                    {
                        if (HexStringToBytes(msg, out List<byte> bytes, out string format))
                        {
                            SendInfo = format;
                            await server.SendDataAsync([.. bytes], clientName);

                        }
                        else
                        {
                            Log("请输入正确的十六进制字符串");
                        }
                    }


                }
            }

        }
        /// <summary>
        /// 发送HexMsgAsync
        /// </summary>
        /// <param name="msg">msg</param>
        /// <returns>返回结果</returns>
        public virtual async Task<bool> SendHexMsgAsync(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                Log($"{DateTime.Now:HH:mm:ss:fff} 消息为空，未发送");
                return false;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            return await SendDataAsync(bytes);
        }
        /// <summary>
        /// 发送MsgAsync
        /// </summary>
        /// <param name="msg">msg</param>
        /// <returns>返回结果</returns>
        public virtual async Task<bool> SendMsgAsync(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                Log($"{DateTime.Now:HH:mm:ss:fff} 消息为空，未发送");
                return false;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            return await SendDataAsync(bytes);
        }
        /// <summary>
        /// 发送数据Async
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <param name="isLog">is日志</param>
        /// <returns>返回结果</returns>
        public virtual async Task<bool> SendDataAsync(byte[] bytes, bool isLog = true)
        {
            await Task.Delay(10);
            return false;

        }
        /// <summary>
        /// 连接Params
        /// </summary>
        public class ConnectParams : BindableBase
        {
            private CommunicationDataType _DataType;
            /// <summary>
            /// 连接配置
            /// </summary>
            public CommunicationDataType DataType
            {
                get { return _DataType; }
                set
                {
                    if (_DataType != value)
                    {
                        Prompt = "已修改，请保存";
                    }
                    SetProperty(ref _DataType, value);
                }

            }


            private string _Prompt;
            /// <summary>
            /// 提示
            /// </summary>
            [JsonIgnore]
            /// <summary>
            /// 提示
            /// </summary>
            public string Prompt
            {
                get { return _Prompt; }
                set { SetProperty(ref _Prompt, value); }
            }
            private byte _UnitId = 1;
            /// <summary>
            /// UnitId
            /// </summary>
            public byte UnitId
            {
                get => _UnitId;
                set => SetProperty(ref _UnitId, value);
            }
            // ─── 串口参数 ───

            private int _BaudRate = 9600;
            /// <summary>
            /// BaudRate
            /// </summary>
            public int BaudRate
            {
                get => _BaudRate;
                set => SetProperty(ref _BaudRate, value);
            }

            private int _DataBits = 8;
            /// <summary>
            /// 数据Bits
            /// </summary>
            public int DataBits
            {
                get => _DataBits;
                set => SetProperty(ref _DataBits, value);
            }

            private System.IO.Ports.StopBits _StopBits = System.IO.Ports.StopBits.One;
            /// <summary>
            /// 停止Bits值
            /// </summary>
            public System.IO.Ports.StopBits StopBitsValue
            {
                get => _StopBits;
                set => SetProperty(ref _StopBits, value);
            }

            private System.IO.Ports.Parity _Parity = System.IO.Ports.Parity.None;
            /// <summary>
            /// Parity值
            /// </summary>
            public System.IO.Ports.Parity ParityValue
            {
                get => _Parity;
                set => SetProperty(ref _Parity, value);
            }

        }
        public enum StateEnum
        {
            Init,
            Receive,
            Sleep
        }
    }

}
