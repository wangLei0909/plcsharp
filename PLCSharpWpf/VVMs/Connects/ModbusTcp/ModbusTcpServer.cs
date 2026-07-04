using PLCSharp.Core.Tools;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PLCSharp.VVMs.Connects.ModbusTcp
{
    /// <summary>
    /// Modbus TCP 服务端 — 监听 TCP 端口，响应 Modbus 请求
    /// 内存中维护线圈、离散输入、保持寄存器、输入寄存器数据表
    /// </summary>
    public class ModbusTcpServer : Connect
    {
        #region Constants

        /// <summary>
        /// FC_READ_COILS
        /// </summary>
        public const byte FC_READ_COILS = 0x01;
        /// <summary>
        /// FC_READ_DISCRETE_INPUTS
        /// </summary>
        public const byte FC_READ_DISCRETE_INPUTS = 0x02;
        /// <summary>
        /// FC_READ_HOLDING_REGISTERS
        /// </summary>
        public const byte FC_READ_HOLDING_REGISTERS = 0x03;
        /// <summary>
        /// FC_READ_INPUT_REGISTERS
        /// </summary>
        public const byte FC_READ_INPUT_REGISTERS = 0x04;
        /// <summary>
        /// FC_WRITE_SINGLE_COIL
        /// </summary>
        public const byte FC_WRITE_SINGLE_COIL = 0x05;
        /// <summary>
        /// FC_WRITE_SINGLE_REGISTER
        /// </summary>
        public const byte FC_WRITE_SINGLE_REGISTER = 0x06;
        /// <summary>
        /// FC_WRITE_MULTIPLE_COILS
        /// </summary>
        public const byte FC_WRITE_MULTIPLE_COILS = 0x0F;
        /// <summary>
        /// FC_WRITE_MULTIPLE_REGISTERS
        /// </summary>
        public const byte FC_WRITE_MULTIPLE_REGISTERS = 0x10;

        // Modbus exception codes
        private const byte EX_ILLEGAL_FUNCTION = 0x01;
        private const byte EX_ILLEGAL_DATA_ADDRESS = 0x02;
        private const byte EX_ILLEGAL_DATA_VALUE = 0x03;
        private const byte EX_SERVER_DEVICE_FAILURE = 0x04;

        private const int MAX_REGISTERS = 65536;

        #endregion

        #region Fields

        private System.Net.Sockets.Socket _socketServer;
        private Thread _acceptThread;
        private readonly BackgroundWorker _bkgWorker;
        private StateEnum State;
        private readonly ConcurrentDictionary<string, System.Net.Sockets.Socket> _clients = new();
        private readonly List<System.Net.Sockets.Socket> _badClients = [];

        #endregion

        #region Modbus Data Store

        // 线圈 (Coils) — 可读写
        private readonly byte[] _coils = new byte[MAX_REGISTERS / 8 + 1];
        // 离散输入 (Discrete Inputs) — 只读
        private readonly byte[] _discreteInputs = new byte[MAX_REGISTERS / 8 + 1];
        // 保持寄存器 (Holding Registers) — 可读写
        private readonly ushort[] _holdingRegisters = new ushort[MAX_REGISTERS];
        // 输入寄存器 (Input Registers) — 只读
        private readonly ushort[] _inputRegisters = new ushort[MAX_REGISTERS];

        /// <summary>
        /// 默认单元标识符
        /// </summary>
        public byte UnitId { get; set; } = 1;

        /// <summary>
        /// 接收事件
        /// </summary>
        public event Action<string> ReceiveEvent;
        public event Action<byte[]> ReceiveDateEvent;

        /// <summary>
        /// 远程客户端写入事件 (type, address, value)
        /// 用于通知 ViewModel 刷新 DataItem 显示
        /// </summary>
        public event Action<ushort, ushort> DataWritten;

        #endregion

        /// <summary>
        /// ModbusTcp服务端
        /// </summary>
        public ModbusTcpServer()
        {
            _bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            _bkgWorker.DoWork += BackgroundWork;

            if (!_bkgWorker.IsBusy)
                _bkgWorker.RunWorkerAsync();
        }

        #region Public Data Access (for simulation / test)

        /// <summary>
        /// 设置线圈值
        /// </summary>
        /// <summary>
        /// 设置Coil
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">值</param>
        public void SetCoil(ushort address, bool value)
        {
            if ((int)address >= MAX_REGISTERS) return;
            if (value)
                _coils[address / 8] |= (byte)(1 << (address % 8));
            else
                _coils[address / 8] &= (byte)~(1 << (address % 8));
        }

        /// <summary>
        /// 读取线圈值
        /// </summary>
        public bool GetCoil(ushort address)
        {
            if ((int)address >= MAX_REGISTERS) return false;
            return (_coils[address / 8] & (1 << (address % 8))) != 0;
        }

        /// <summary>
        /// 设置离散输入值
        /// </summary>
        public void SetDiscreteInput(ushort address, bool value)
        {
            if ((int)address >= MAX_REGISTERS) return;
            if (value)
                _discreteInputs[address / 8] |= (byte)(1 << (address % 8));
            else
                _discreteInputs[address / 8] &= (byte)~(1 << (address % 8));
        }

        /// <summary>
        /// 设置保持寄存器值
        /// </summary>
        public void SetHoldingRegister(ushort address, ushort value)
        {
            if ((int)address >= MAX_REGISTERS) return;
            _holdingRegisters[address] = value;
        }

        /// <summary>
        /// 读取保持寄存器值
        /// </summary>
        public ushort GetHoldingRegister(ushort address)
        {
            if ((int)address >= MAX_REGISTERS) return 0;
            return _holdingRegisters[address];
        }

        /// <summary>
        /// 设置输入寄存器值
        /// </summary>
        public void SetInputRegister(ushort address, ushort value)
        {
            if ((int)address >= MAX_REGISTERS) return;
            _inputRegisters[address] = value;
        }

        #endregion

        #region Connection

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool Connect()
        {
            try
            {
                IPEndPoint iPEndPoint = new(IPAddress.Parse(IP_SerialPort), Port);

                if (_socketServer != null && _socketServer.LocalEndPoint?.ToString() == $"{IP_SerialPort}:{Port}")
                    return true;

                if (_socketServer != null)
                {
                    _socketServer.Close();
                    _acceptThread?.Interrupt();
                }

                _socketServer = new(AddressFamily.InterNetwork, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                _socketServer.Bind(iPEndPoint);
                _socketServer.Listen();

                _acceptThread = new Thread(new ThreadStart(AcceptClientConnect))
                {
                    IsBackground = true
                };
                _acceptThread.Start();

                Log($"{DateTime.Now:HH:mm:ss:fff} ModbusTCP 服务端 {IP_SerialPort}:{Port} 已启动");
                return true;
            }
            catch (Exception ex)
            {
                Log($"{DateTime.Now:HH:mm:ss:fff} ModbusTCP 服务端启动失败: {ex.Message}");
                _socketServer?.Dispose();
                _socketServer = null;
                return false;
            }
        }

        private void AcceptClientConnect()
        {
            Log($"{DateTime.Now:HH:mm:ss:fff} ModbusTCP 服务端 {_socketServer.LocalEndPoint} 等待客户端...");

            while (true)
            {
                try
                {
                    Thread.Sleep(100);
                    System.Net.Sockets.Socket proxSocket = _socketServer.Accept();
                    if (proxSocket == null) continue;

                    var clientKey = proxSocket.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
                    _clients.TryAdd(clientKey, proxSocket);

                    Log($"{DateTime.Now:HH:mm:ss:fff} ModbusTCP 客户端 {clientKey} 已连接");

                    var client = Clients.FirstOrDefault(c => c.IP_SerialPort == clientKey.Split(":")[0]);
                    if (client != null) client.Connected = true;

                    _ = Task.Run(() => HandleClient(proxSocket, clientKey));
                }
                catch (Exception ex)
                {
                    Log($"ModbusTCP Accept: {ex.Message}");
                }
            }
        }

        private void HandleClient(System.Net.Sockets.Socket proxSocket, string clientKey)
        {
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    int len = proxSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    if (len <= 0)
                    {
                        ClientDisconnected(clientKey, proxSocket);
                        return;
                    }

                    byte[] request = buffer[..len];
                    var rxStr = BitConverter.ToString(request).Replace("-", " ");
                    Log($"{DateTime.Now:HH:mm:ss:fff} ModbusRx [{clientKey}] [{rxStr}]");

                    // 尝试处理 Modbus 请求
                    byte[] response = ProcessModbusRequest(request);
                    if (response != null)
                    {
                        proxSocket.Send(response, 0, response.Length, SocketFlags.None);
                        var txStr = BitConverter.ToString(response).Replace("-", " ");
                        Log($"{DateTime.Now:HH:mm:ss:fff} ModbusTx [{clientKey}] [{txStr}]");

                        ReceiveData = request;
                        ReceiveDateEvent?.Invoke(request);
                    }
                }
                catch (Exception ex)
                {
                    ClientDisconnected(clientKey, proxSocket, ex.Message);
                    return;
                }
            }
        }

        private void ClientDisconnected(string clientKey, System.Net.Sockets.Socket proxSocket, string reason = "")
        {
            string msg = string.IsNullOrEmpty(reason)
                ? $"{DateTime.Now:HH:mm:ss:fff} 客户端 {clientKey} 断开"
                : $"{DateTime.Now:HH:mm:ss:fff} 客户端 {clientKey} 异常断开: {reason}";
            Log(msg);

            _badClients.Add(proxSocket);
            ClearBad();

            var client = Clients.FirstOrDefault(c => c.IP_SerialPort == clientKey.Split(":")[0]);
            if (client != null) client.Connected = false;

            _clients.TryRemove(clientKey, out _);
        }

        #endregion

        #region Modbus Protocol Processing

        private byte[] ProcessModbusRequest(byte[] request)
        {
            if (request.Length < 8) // MBAP(7) + FC(1)
                return null;

            try
            {
                // 提取 MBAP 头
                ushort transactionId = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(request, 0));
                ushort protocolId = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(request, 2));
                ushort length = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(request, 4));
                byte unitId = request[6];

                // 验证协议标识符
                if (protocolId != 0)
                    return null;

                byte functionCode = request[7];

                // PDU 数据 (从 index 8 开始)
                byte[] pduData = request.Length > 8 ? request[8..] : [];

                // 处理功能码并生成响应 PDU
                byte[] responsePdu = ExecuteFunction(functionCode, pduData);
                if (responsePdu == null)
                    return null;

                // 构建响应 MBAP + PDU
                ushort respLength = (ushort)(1 + responsePdu.Length); // unitId(1) + PDU
                using MemoryStream ms = new();
                ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)transactionId)), 0, 2);
                ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)0)), 0, 2);
                ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)respLength)), 0, 2);
                ms.WriteByte(unitId);
                ms.Write(responsePdu, 0, responsePdu.Length);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                Log($"Modbus 处理异常: {ex.Message}");
                return null;
            }
        }

        private byte[] ExecuteFunction(byte functionCode, byte[] data)
        {
            switch (functionCode)
            {
                case FC_READ_COILS: return ReadCoils(data);
                case FC_READ_DISCRETE_INPUTS: return ReadDiscreteInputs(data);
                case FC_READ_HOLDING_REGISTERS: return ReadHoldingRegisters(data);
                case FC_READ_INPUT_REGISTERS: return ReadInputRegisters(data);
                case FC_WRITE_SINGLE_COIL: return WriteSingleCoil(data);
                case FC_WRITE_SINGLE_REGISTER: return WriteSingleRegister(data);
                case FC_WRITE_MULTIPLE_COILS: return WriteMultipleCoils(data);
                case FC_WRITE_MULTIPLE_REGISTERS: return WriteMultipleRegisters(data);
                default:
                    return BuildExceptionResponse(functionCode, EX_ILLEGAL_FUNCTION);
            }
        }

        private byte[] ReadCoils(byte[] data)
        {
            // data: startAddress(2) + quantity(2)
            if (data.Length < 4)
                return BuildExceptionResponse(FC_READ_COILS, EX_ILLEGAL_DATA_ADDRESS);

            ushort startAddr = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            ushort quantity = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));

            if (quantity < 1 || quantity > 2000 || (int)startAddr + quantity > MAX_REGISTERS)
                return BuildExceptionResponse(FC_READ_COILS, EX_ILLEGAL_DATA_ADDRESS);

            int byteCount = (quantity + 7) / 8;
            byte[] response = new byte[2 + byteCount];
            response[0] = FC_READ_COILS;
            response[1] = (byte)byteCount;

            for (int i = 0; i < quantity; i++)
            {
                int addr = startAddr + i;
                if ((_coils[addr / 8] & (1 << (addr % 8))) != 0)
                    response[2 + i / 8] |= (byte)(1 << (i % 8));
            }
            return response;
        }

        private byte[] ReadDiscreteInputs(byte[] data)
        {
            if (data.Length < 4)
                return BuildExceptionResponse(FC_READ_DISCRETE_INPUTS, EX_ILLEGAL_DATA_ADDRESS);

            ushort startAddr = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            ushort quantity = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));

            if (quantity < 1 || quantity > 2000 || (int)startAddr + quantity > MAX_REGISTERS)
                return BuildExceptionResponse(FC_READ_DISCRETE_INPUTS, EX_ILLEGAL_DATA_ADDRESS);

            int byteCount = (quantity + 7) / 8;
            byte[] response = new byte[2 + byteCount];
            response[0] = FC_READ_DISCRETE_INPUTS;
            response[1] = (byte)byteCount;

            for (int i = 0; i < quantity; i++)
            {
                int addr = startAddr + i;
                if ((_discreteInputs[addr / 8] & (1 << (addr % 8))) != 0)
                    response[2 + i / 8] |= (byte)(1 << (i % 8));
            }
            return response;
        }

        private byte[] ReadHoldingRegisters(byte[] data)
        {
            if (data.Length < 4)
                return BuildExceptionResponse(FC_READ_HOLDING_REGISTERS, EX_ILLEGAL_DATA_ADDRESS);

            ushort startAddr = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            ushort quantity = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));

            if (quantity < 1 || quantity > 125 || (int)startAddr + quantity > MAX_REGISTERS)
                return BuildExceptionResponse(FC_READ_HOLDING_REGISTERS, EX_ILLEGAL_DATA_ADDRESS);

            int byteCount = quantity * 2;
            byte[] response = new byte[2 + byteCount];
            response[0] = FC_READ_HOLDING_REGISTERS;
            response[1] = (byte)byteCount;

            for (int i = 0; i < quantity; i++)
            {
                ushort val = _holdingRegisters[startAddr + i];
                Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)val)), 0, response, 2 + i * 2, 2);
            }
            return response;
        }

        private byte[] ReadInputRegisters(byte[] data)
        {
            if (data.Length < 4)
                return BuildExceptionResponse(FC_READ_INPUT_REGISTERS, EX_ILLEGAL_DATA_ADDRESS);

            ushort startAddr = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            ushort quantity = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));

            if (quantity < 1 || quantity > 125 || (int)startAddr + quantity > MAX_REGISTERS)
                return BuildExceptionResponse(FC_READ_INPUT_REGISTERS, EX_ILLEGAL_DATA_ADDRESS);

            int byteCount = quantity * 2;
            byte[] response = new byte[2 + byteCount];
            response[0] = FC_READ_INPUT_REGISTERS;
            response[1] = (byte)byteCount;

            for (int i = 0; i < quantity; i++)
            {
                ushort val = _inputRegisters[startAddr + i];
                Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)val)), 0, response, 2 + i * 2, 2);
            }
            return response;
        }

        private byte[] WriteSingleCoil(byte[] data)
        {
            if (data.Length < 4)
                return BuildExceptionResponse(FC_WRITE_SINGLE_COIL, EX_ILLEGAL_DATA_ADDRESS);

            ushort address = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            ushort value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));

            if ((int)address >= MAX_REGISTERS)
                return BuildExceptionResponse(FC_WRITE_SINGLE_COIL, EX_ILLEGAL_DATA_ADDRESS);
            if (value != 0xFF00 && value != 0x0000)
                return BuildExceptionResponse(FC_WRITE_SINGLE_COIL, EX_ILLEGAL_DATA_VALUE);

            SetCoil(address, value == 0xFF00);
            DataWritten?.Invoke(address, value == 0xFF00 ? (ushort)1 : (ushort)0);

            // Echo request as response
            byte[] resp = new byte[3];
            resp[0] = FC_WRITE_SINGLE_COIL;
            Array.Copy(data, 0, resp, 1, 2);
            return resp;
        }

        private byte[] WriteSingleRegister(byte[] data)
        {
            if (data.Length < 4)
                return BuildExceptionResponse(FC_WRITE_SINGLE_REGISTER, EX_ILLEGAL_DATA_ADDRESS);

            ushort address = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            ushort value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));

            if ((int)address >= MAX_REGISTERS)
                return BuildExceptionResponse(FC_WRITE_SINGLE_REGISTER, EX_ILLEGAL_DATA_ADDRESS);

            _holdingRegisters[address] = value;
            DataWritten?.Invoke(address, value);

            byte[] resp = new byte[5];
            resp[0] = FC_WRITE_SINGLE_REGISTER;
            Array.Copy(data, 0, resp, 1, 4);
            return resp;
        }

        private byte[] WriteMultipleCoils(byte[] data)
        {
            if (data.Length < 6)
                return BuildExceptionResponse(FC_WRITE_MULTIPLE_COILS, EX_ILLEGAL_DATA_ADDRESS);

            ushort startAddr = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            ushort quantity = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));
            byte byteCount = data[4];

            if ((int)startAddr + quantity > MAX_REGISTERS)
                return BuildExceptionResponse(FC_WRITE_MULTIPLE_COILS, EX_ILLEGAL_DATA_ADDRESS);
            if (data.Length < 5 + byteCount)
                return BuildExceptionResponse(FC_WRITE_MULTIPLE_COILS, EX_ILLEGAL_DATA_VALUE);

            for (int i = 0; i < quantity; i++)
            {
                int addr = startAddr + i;
                bool val = (data[5 + i / 8] & (1 << (i % 8))) != 0;
                SetCoil((ushort)addr, val);
                DataWritten?.Invoke((ushort)addr, val ? (ushort)1 : (ushort)0);
            }

            byte[] resp = new byte[5];
            resp[0] = FC_WRITE_MULTIPLE_COILS;
            Array.Copy(data, 0, resp, 1, 4);
            return resp;
        }

        private byte[] WriteMultipleRegisters(byte[] data)
        {
            if (data.Length < 6)
                return BuildExceptionResponse(FC_WRITE_MULTIPLE_REGISTERS, EX_ILLEGAL_DATA_ADDRESS);

            ushort startAddr = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
            ushort quantity = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));
            byte byteCount = data[4];

            if ((int)startAddr + quantity > MAX_REGISTERS)
                return BuildExceptionResponse(FC_WRITE_MULTIPLE_REGISTERS, EX_ILLEGAL_DATA_ADDRESS);
            if (data.Length < 5 + byteCount)
                return BuildExceptionResponse(FC_WRITE_MULTIPLE_REGISTERS, EX_ILLEGAL_DATA_VALUE);

            for (int i = 0; i < quantity; i++)
            {
                int offset = 5 + i * 2;
                if (offset + 1 < data.Length)
                {
                    ushort val = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, offset));
                    _holdingRegisters[startAddr + i] = val;
                    DataWritten?.Invoke((ushort)(startAddr + i), val);
                }
            }

            byte[] resp = new byte[5];
            resp[0] = FC_WRITE_MULTIPLE_REGISTERS;
            Array.Copy(data, 0, resp, 1, 4);
            return resp;
        }

        private byte[] BuildExceptionResponse(byte functionCode, byte exceptionCode)
        {
            return [(byte)(functionCode | 0x80), exceptionCode];
        }

        #endregion

        #region Background Worker

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

        #endregion

        #region Send/Receive Override

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

            if (_clients.IsEmpty)
            {
                Log("ModbusTCP 服务端: 没有客户端连接");
                Successful = false;
                return false;
            }

            foreach (var kvp in _clients)
            {
                try
                {
                    if (kvp.Value.Connected)
                    {
                        await kvp.Value.SendAsync(bytes.AsMemory(0, bytes.Length));
                        if (isLog)
                        {
                            var bytesStr = BitConverter.ToString(bytes).Replace("-", " ");
                            Log($"{DateTime.Now:HH:mm:ss:fff} ModbusTx [{kvp.Key}] [{bytesStr}]");
                        }
                    }
                    else
                    {
                        _badClients.Add(kvp.Value);
                        ClearBad();
                    }
                }
                catch
                {
                    _badClients.Add(kvp.Value);
                    ClearBad();
                }
            }

            Successful = true;
            return true;
        }

        #endregion

        #region Cleanup

        private void ClearBad()
        {
            foreach (var proxSocket in _badClients)
            {
                string key = _clients.FirstOrDefault(kv => kv.Value == proxSocket).Key;
                if (key != null)
                    _clients.TryRemove(key, out _);

                try
                {
                    if (proxSocket.Connected)
                    {
                        proxSocket.Shutdown(SocketShutdown.Both);
                        proxSocket.Close(100);
                    }
                }
                catch { }
            }
            _badClients.Clear();
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public override void Close()
        {
            _bkgWorker.CancelAsync();
            _bkgWorker.Dispose();
            _socketServer?.Close();

            foreach (var kvp in _clients)
            {
                _badClients.Add(kvp.Value);
            }
            ClearBad();
            _acceptThread?.Interrupt();
        }

        #endregion
    }
}
