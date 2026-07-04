using PLCSharp.Core.Tools;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PLCSharp.VVMs.Connects.Socket
{
    /// <summary>
    /// Socket服务端
    /// </summary>
    public class SocketServer : Connect
    {
        private readonly List<System.Net.Sockets.Socket> ClientList = [];// 客户端
        private readonly List<System.Net.Sockets.Socket> BadClientList = [];//失效 的 客户端
        private Thread thread;
        private System.Net.Sockets.Socket socketServer;
        private readonly BackgroundWorker bkgWorker;

        private StateEnum State;
        /// <summary>
        /// Socket服务端
        /// </summary>
        public SocketServer()

        {

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();
        }

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                Thread.Sleep(10);
                try
                {
                    switch (State)
                    {
                        case StateEnum.Init:
                            {
                                if (Connect() && Online)
                                {
                                    State = StateEnum.Sleep;
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
                        case StateEnum.Sleep:
                            {
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
        /// 连接
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool Connect()
        {
            try
            {

                IPEndPoint iPEndPoint = new(IPAddress.Parse(IP_SerialPort), Port);

                if (socketServer != null && socketServer.LocalEndPoint.ToString() == $"{IP_SerialPort}:{Port}")
                {
                    return true;
                }
                if (socketServer != null)
                {
                    socketServer.Close();
                    foreach (var proxSocket in ClientList)
                    {
                        if (!BadClientList.Contains(proxSocket))
                        {
                            BadClientList.Add(proxSocket);
                        }
                    }
                    ClearBad();
                    thread?.Interrupt();
                }

                //1、创建Socket对象 参数：寻址方式，当前为Ipv4  指定套接字类型   指定传输协议Tcp；
                socketServer = new(AddressFamily.InterNetwork, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                //2、绑定端口、IP
                socketServer.Bind(iPEndPoint);

                //3、开启侦听   10为队列最多接收的数量
                socketServer.Listen();

                //4、开始接受客户端的连接  ，连接会阻塞主线程，故使用线程池。
                thread = new Thread(new ThreadStart(AcceptClientConnect))
                {
                    IsBackground = true
                };
                thread.Start();

                return true;

            }
            catch (Exception ex)
            {
                //    throw;
                Log($"{DateTime.Now:HH:mm:ss:fff} {ex.Message}");
                socketServer.Dispose();
                socketServer = null;
                return false;

            }
        }
        /// <summary>
        ///关闭连接
        /// </summary>
        public override void Close()
        {
            bkgWorker.CancelAsync();
            bkgWorker.Dispose();
            socketServer.Close();
            foreach (var proxSocket in ClientList)
            {
                if (!BadClientList.Contains(proxSocket))
                {
                    BadClientList.Add(proxSocket);
                }
            }
            ClearBad();
            thread?.Interrupt();
        }
        private void AcceptClientConnect()
        {
            //转换Socket

            Log($"{DateTime.Now:HH:mm:ss:fff} 服务端{socketServer.LocalEndPoint}已准备好！");

            //不断接受客户端的连接
            while (true)
            {
                try
                {
                    Thread.Sleep(100);
                    //5、创建一个负责通信的Socket

                    System.Net.Sockets.Socket proxSocket = socketServer.Accept();
                    if (proxSocket == null)
                        continue;

                    var clientIP = proxSocket.RemoteEndPoint.ToString();
                    Log($"{DateTime.Now:HH:mm:ss:fff} 客户端：{clientIP} 连接上了！");
                    var client = Clients.FirstOrDefault(c => c.IP_SerialPort == clientIP.Split(":")[0]);
                    if (client != null)
                    {
                        client.Connected = true;
                    }
                    //将连接的Socket存入集合

                    ClientList.Add(proxSocket);
                    //6、不断接收客户端发送来的消息
                    _ = Task.Run(() => ReceiveClientMsg(proxSocket));
                }
                catch (Exception ex)
                {

                    Log($"SendMsg:{ex.Message}");

                }
            }
        }



        private void ReceiveClientMsg(System.Net.Sockets.Socket proxSocket)
        {
            var clientIP = proxSocket.RemoteEndPoint.ToString();
            var client = Clients.FirstOrDefault(c => c.IP_SerialPort == clientIP.Split(":")[0]);
            //创建缓存内存，存储接收的信息, 不能放到while中，这块内存可以循环利用
            byte[] data = new byte[1024 * 1024];
            while (true)
            {
                int len;
                try
                {
                    //接收消息,返回字节长度
                    len = proxSocket.Receive(data, 0, data.Length, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    //7、关闭Socket

                    ClientExit($"{DateTime.Now:HH:mm:ss:fff}  客户端：{clientIP}异常常退出：{ex.Message}", proxSocket);
                    if (client != null)
                    {
                        client.Connected = false;
                    }
                    return;//让方法结束，终结当前客户端数据的异步线程，方法退出，即线程结束
                }

                if (len <= 0)//判断接收的字节数   小于0表示正常退出
                {
                    ClientExit($"{DateTime.Now:HH:mm:ss:fff}  客户端：{clientIP}正常退出", proxSocket);
                    if (client != null)
                    {
                        client.Connected = false;
                    }
                    return;//让方法结束，终结当前客户端数据的异步线程，方法退出，即线程结束
                }



                if (Params.DataType == CommunicationDataType.Bytes)
                {
                    var bytesStr = BitConverter.ToString(data, 0, len);

                    ReceiveData = data[..len];
                    if (client != null)
                    {
                        client.ReceiveData = data[..len];
                    }
                    Log($"{DateTime.Now:HH:mm:ss:fff} 接收{IP_SerialPort}信息 <=={bytesStr}");
                }
                else
                {
                    string msgStr = Encoding.UTF8.GetString(data, 0, len);
                    if (string.IsNullOrEmpty(msgStr))
                    {
                        return;
                    }
                    ReceiveInfo = msgStr;
                    if (client != null)
                    {
                        client.ReceiveInfo = msgStr;

                    }
                    Log($"{DateTime.Now:HH:mm:ss:fff} 接收到[{clientIP}]的消息<==：{msgStr.Replace("\r\n", "\\r\\n")}");
                }

            }
        }

        /// <summary>
        /// 消息广播
        /// </summary>
        public bool SendMsg(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(str);

            SendData(bytes);

            return true;
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


            if (ClientList.Count > 0)
            {

                foreach (var proxSocket in ClientList)
                {
                    if (proxSocket.Connected)//判断客户端是否还在连接
                    {
                        _ = proxSocket.Send(bytes, 0, bytes.Length, SocketFlags.None); //指定套接字的发送行为

                        if (isLog)
                        {

                            var bytesStr = BitConverter.ToString(bytes).Replace("-", " ");
                            Log($"{DateTime.Now:HH:mm:ss:fff}向{proxSocket.RemoteEndPoint}发送信息 ==>{bytesStr}");



                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(bytes);
                            Log($"{DateTime.Now:HH:mm:ss:fff}向{proxSocket.RemoteEndPoint}发送信息 ==>{str}");
                        }


                    }
                    else
                    {
                        BadClientList.Add(proxSocket);
                        Log("SendMsg:客户端未连接");
                        ClearBad();

                    }
                }
                Successful = true;
                return true;
            }
            else
            {
                Log("SendMsg:没有客户机接入");
                Successful = false;
                return false;
            }


        }

        /// <summary>
        /// 消息广播
        /// </summary>
        public override async Task<bool> SendMsgAsync(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                Log("SendMsg:消息为空，未发送");
                return false;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(str);

            await SendDataAsync(bytes, false);

            return true;
        }
        /// <summary>
        /// 发送数据Async
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <param name="isLog">is日志</param>
        /// <returns>返回结果</returns>
        public override async Task<bool> SendDataAsync(byte[] bytes, bool isLog = true)
        {
            if (bytes == null || bytes.Length == 0)
            {
                Successful = false;
                return false;
            }


            if (ClientList.Count > 0)
            {

                foreach (var proxSocket in ClientList)
                {
                    if (proxSocket.Connected)//判断客户端是否还在连接
                    {
                        await proxSocket.SendAsync(bytes.AsMemory(0, bytes.Length));
                        if (isLog)
                        {
                            var bytesStr = BitConverter.ToString(bytes).Replace("-", " ");
                            Log($"{DateTime.Now:HH:mm:ss:fff}向{proxSocket.RemoteEndPoint}发送信息 ==>{bytesStr}");
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(bytes);
                            Log($"{DateTime.Now:HH:mm:ss:fff}向{proxSocket.RemoteEndPoint}发送信息 ==>{str}");
                        }
                    }
                    else
                    {
                        BadClientList.Add(proxSocket);
                        Log("SendMsg:客户端未连接");
                        ClearBad();
                    }
                }
                Successful = true;
                return true;
            }
            else
            {
                Log("SendMsg:没有客户机接入");
                Successful = false;
                return false;
            }


        }

        /// <summary>
        /// 发送MsgAsync
        /// </summary>
        /// <param name="str">str</param>
        /// <param name="clientName">客户端名称</param>
        /// <returns>返回结果</returns>
        public async Task<bool> SendMsgAsync(string str, string clientName)
        {
            if (string.IsNullOrEmpty(str))
            {
                Log("SendMsg:消息为空，未发送");

                return false;
            }
            var client = Clients.FirstOrDefault(c => c.Name == clientName);
            if (client == null)
            {
                Log($"SendMsg:{clientName}未连接");

                return false;
            }
            var proxSocket = ClientList.FirstOrDefault(c => c.RemoteEndPoint.ToString().Split(":")[0] == client.IP_SerialPort);
            if (proxSocket == null)
            {
                client.Connected = false;
                Log($"SendMsg:{clientName}未连接");
                return false;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            await SendDataAsync(bytes, clientName, false);

            return true;
        }
        /// <summary>
        /// 发送数据Async
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <param name="clientName">客户端名称</param>
        /// <param name="isLog">is日志</param>
        /// <returns>返回结果</returns>
        public async Task<bool> SendDataAsync(byte[] bytes, string clientName, bool isLog = true)
        {
            if (bytes == null || bytes.Length == 0)
            {
                Log("SendMsg:消息为空，未发送");

                Successful = false;
                return false;
            }


            if (ClientList.Count > 0)
            {
                var client = Clients.FirstOrDefault(c => c.Name == clientName);
                if (client == null)
                {
                    Log($"SendMsg:{clientName}未连接");

                    return false;
                }
                var proxSocket = ClientList.FirstOrDefault(c => c.RemoteEndPoint.ToString().Split(":")[0] == client.IP_SerialPort);
                if (proxSocket == null)
                {
                    client.Connected = false;
                    Log($"SendMsg:{clientName}未连接");

                    return false;
                }
                if (proxSocket.Connected)//判断客户端是否还在连接
                {
                    await proxSocket.SendAsync(bytes.AsMemory(0, bytes.Length));
                    if (isLog)
                    {
                        var bytesStr = BitConverter.ToString(bytes).Replace("-", " ");
                        Log($"{DateTime.Now:HH:mm:ss:fff}向{proxSocket.RemoteEndPoint}发送信息 ==>{bytesStr}");
                    }
                    else
                    {
                        var str = Encoding.UTF8.GetString(bytes);
                        Log($"{DateTime.Now:HH:mm:ss:fff}向{proxSocket.RemoteEndPoint}发送信息 ==>{str}");
                    }
                }
                else
                {
                    BadClientList.Add(proxSocket);
                    Log("SendMsg:客户端未连接");
                    ClearBad();
                }

                Successful = true;
                return true;
            }
            else
            {
                Log("SendMsg:没有客户机接入");
                Successful = false;
                return false;
            }


        }
        private void ClearBad()
        {
            foreach (var proxSocket in BadClientList)
            {
                if (ClientList.Contains(proxSocket))
                {
                    if (proxSocket.Connected)//如果是连接状态
                    {
                        proxSocket.Shutdown(SocketShutdown.Both);//关闭连接
                        proxSocket.Close(100);//100秒超时间
                    }
                    ClientList.Remove(proxSocket);
                }
            }
            BadClientList.Clear();
        }

        /// <summary>
        /// 向指定客户端发送消息
        /// </summary>
        /// <param name="str"></param>
        /// <param name="proxSocket"></param>
        /// <returns></returns>
        public bool SendMsg(string str, System.Net.Sockets.Socket proxSocket)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            if (proxSocket.Connected)//判断客户端是否还在连接
            {
                byte[] data = Encoding.UTF8.GetBytes(str);

                _ = proxSocket.Send(data, 0, data.Length, SocketFlags.None); //指定套接字的发送行为
                string clinet = proxSocket.RemoteEndPoint.ToString();
                Log($"{DateTime.Now:HH:mm:ss:fff} 向连接[{clinet}]发信息=>:{str}");
                return true;
            }
            else
            {
                Log($"{proxSocket}未连接");
                BadClientList.Add(proxSocket);
                ClearBad();
                return false;
            }
        }
        /// <summary>
        /// 向指定客户端发送消息
        /// </summary>
        /// <param name="str"></param>
        /// <param name="proxSocket"></param>
        /// <returns></returns>
        public bool SendMsg(string str, string clientName)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }
            var client = Clients.FirstOrDefault(c => c.Name == clientName);

            var proxSocket = ClientList.FirstOrDefault(x => x.RemoteEndPoint.ToString().Split(":")[0] == client.IP_SerialPort);


            if (proxSocket.Connected)//判断客户端是否还在连接
            {
                byte[] data = Encoding.UTF8.GetBytes(str);

                _ = proxSocket.Send(data, 0, data.Length, SocketFlags.None); //指定套接字的发送行为
                string clinet = proxSocket.RemoteEndPoint.ToString();
                Log($"{DateTime.Now:HH:mm:ss:fff} 向连接[{clinet}]发信息=>:{str}");
                return true;
            }
            else
            {
                Log($"{proxSocket}未连接");
                BadClientList.Add(proxSocket);
                ClearBad();
                return false;
            }
        }
        private void ClientExit(string msg, System.Net.Sockets.Socket proxSocket)
        {
            Log(msg);
            BadClientList.Add(proxSocket);//移除集合中的连接Socket
            ClearBad();
        }

    }
}
