using PLCSharp.Core.Tools;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace PLCSharp.VVMs.Connects.Socket
{
    /// <summary>
    /// Socket客户端
    /// </summary>
    public class SocketClient : Connect
    {

        private TcpClient _client;

        private readonly BackgroundWorker bkgWorker;
        /// <summary>
        /// Socket客户端
        /// </summary>
        public SocketClient()

        {

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();
        }



        public event Action<string> ReceiveEvent;

        public event Action<byte[]> ReceiveDateEvent;

        private StateEnum State;

        /// <summary>
        /// 连接
        /// </summary>
        protected bool Connect()
        {
            lock (this)
            {
                if (IP_SerialPort == null) return false;
                if (IPAddress.TryParse(IP_SerialPort, out _) == false) return false;
                bool success = false;
                _client?.Close();
                _client = new TcpClient();


                try
                {
                    _client.Connect(IP_SerialPort, Port);
                    success = _client.Connected;
                    Log($"{DateTime.Now} ;{IP_SerialPort}:{Port}连接成功");

                }
                catch (Exception ex)
                {
                    Log($"{IP_SerialPort}:{Port}无法连接...,{ex.Message}");
                }
                return success;
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public override async Task<bool> SendMsgAsync(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                Log($"{DateTime.Now:HH:mm:ss:fff} 消息为空，未发送");
                Successful = false;
                return false;
            }
            byte[] byteData = Encoding.UTF8.GetBytes(msg);
            bool isLog = false;
            if (Params.DataType == CommunicationDataType.Bytes) isLog = true;
            var done = await SendDataAsync(byteData, isLog);
            if (Params.DataType == CommunicationDataType.String && done)
                Log($"{DateTime.Now:HH:mm:ss:fff}向{IP_SerialPort}发送信息 ==>{msg}");

            Successful = done;
            return done;
        }
        /// <summary>
        /// 发送数据Async
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <param name="isLog">is日志</param>
        /// <returns>返回结果</returns>
        public override async Task<bool> SendDataAsync(byte[] bytes, bool isLog = true)
        {
            if (bytes == null || bytes.Length == 0) return false;

            if (_client != null && _client.Connected)
            {
                try
                {
                    await _client.GetStream().WriteAsync(bytes.AsMemory(0, bytes.Length));
                    if (isLog)
                    {
                        var bytesStr = BitConverter.ToString(bytes).Replace("-", " ");
                        Log($"{DateTime.Now:HH:mm:ss:fff}向{IP_SerialPort}发送信息 ==>{bytesStr}");
                    }
                    Successful = true;
                    return true;
                }
                catch (Exception ex)
                {
                    Log($"SendMsg Exception: {ex.Message}");
                    Successful = false;
                    return false;
                }

            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public bool SendMsg(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                Log($"{DateTime.Now:HH:mm:ss:fff} 消息为空，未发送");
                Successful = false;
                return false;
            }
            byte[] byteData = Encoding.UTF8.GetBytes(msg);
            bool isLog = false;
            if (Params.DataType == CommunicationDataType.Bytes)
            {
                isLog = true;
            }
            var done = SendData(byteData, isLog);
            if (Params.DataType == CommunicationDataType.String && done)
            {
                Log($"{DateTime.Now:HH:mm:ss:fff}向{IP_SerialPort}发送信息 ==>{msg}");
            }
            Successful = done;
            return done;
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <param name="isLog">is日志</param>
        /// <returns>返回布尔值</returns>
        public bool SendData(byte[] bytes, bool isLog = true)
        {
            if (bytes == null || bytes.Length == 0)
            {
                Successful = false;
                return false;
            }

            if (_client != null && _client.Connected)
            {
                try
                {
                    _client.GetStream().Write(bytes);
                    if (isLog)
                    {
                        var bytesStr = BitConverter.ToString(bytes).Replace("-", " ");
                        Log($"{DateTime.Now:HH:mm:ss:fff}向{IP_SerialPort}发送信息 ==>{bytesStr}");
                    }
                    Successful = true;

                    return true;
                }
                catch (Exception ex)
                {
                    Log($"SendMsg Exception: {ex.Message}");
                    Successful = false;
                    return false;
                }

            }
            else
            {
                Successful = false;
                return false;
            }
        }


        /// <summary>
        /// 接收Bytes
        /// </summary>
        public byte[] ReceiveBytes { get; private set; } = [];



        private readonly byte[] _buff = new byte[1024];

        private bool ReceiveMsg()
        {
            if (_client != null && _client.Connected)
            {
                //下面这个指令会造成m_buffRece数据丢失！
                //Array.Clear(m_buffRece, 0, m_buffRece.Length);
                try
                {
                    var receiveDataLen = _client.GetStream().Read(_buff, 0, _buff.Length);

                    // Thread.Sleep(100); // 这个没影响
                    // await Task.Delay(100); //会丢数据,因为 await 之后，接管的线程不一定是原来的线程

                    //服务器异常断开会一直收到 0 长度的信息
                    if (receiveDataLen == 0)
                    {
                        Successful = false;
                        return false;
                    }
                    if (Params.DataType == CommunicationDataType.Bytes)
                    {
                        ReceiveBytes = _buff[..receiveDataLen];
                        ReceiveDateEvent?.Invoke(ReceiveBytes);

                        var bytesStr = BitConverter.ToString(ReceiveBytes);

                        Log($"{DateTime.Now:HH:mm:ss:fff} 接收{IP_SerialPort}信息 <=={bytesStr}");

                    }
                    else
                    {
                        var msg = Encoding.UTF8.GetString(_buff, 0, receiveDataLen);
                        ReceiveInfo = msg;
                        ReceiveEvent?.Invoke(msg);

                        Log($"{DateTime.Now:HH:mm:ss:fff} 接收{IP_SerialPort}信息 <== {msg}");

                    }

                    //以上两个指令必须一起执行，中间不能有异步操作，否则_buff里的数据会丢失！！！
                    Successful = true;

                    return true;
                }
                catch (Exception ex)
                {
                    Log("ReceMsg Exception: " + ex.Message);
                    Successful = false;

                    return false;
                }
            }
            else
            {
                Successful = false;
                return false;
            }
        }
        /// <summary>
        /// BackgroundWork
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        protected virtual void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                try
                {
                    switch (State)
                    {
                        case StateEnum.Init:
                            {
                                if (Connect() && Online)
                                {
                                    State = StateEnum.Receive;
                                    Connected = true;
                                }
                                else
                                {
                                    State = StateEnum.Sleep;
                                    Connected = false;
                                    Successful = false;
                                }
                            }
                            break;
                        case StateEnum.Receive:
                            {
                                if (ReceiveMsg())
                                {
                                    break;
                                }
                                else
                                {
                                    State = StateEnum.Sleep;
                                    break;
                                }
                            }

                        case StateEnum.Sleep:
                            {
                                _client?.Close();
                                Thread.Sleep(1000);
                                Online = NetTool.PingIP(IP_SerialPort);
                                State = StateEnum.Init;
                            }
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Log($"Error: {ex.Message}");
                    Thread.Sleep(1);
                }


            }
        }


        /// <summary>
        /// 获取Stream
        /// </summary>
        /// <returns>返回结果</returns>
        public NetworkStream GetStream()
        {
            return _client?.GetStream();
        }
        /// <summary>
        ///关闭连接
        /// </summary>
        public override void Close()
        {
            bkgWorker.CancelAsync();
            bkgWorker.Dispose();
            _client?.Close();
        }



    }



}
