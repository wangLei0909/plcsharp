using PLCSharp.Core.Tools;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PLCSharp.VVMs.Connects.ModbusTcp
{
    /// <summary>
    /// Modbus TCP 客户端 — 直接继承 Connect，自管理 TcpClient
    /// 自动连接 / IP变更重连，无 BackgroundWorker 干扰
    /// </summary>
    public class ModbusTcpClient : Connect
    {
        /// <summary>
        /// FC_READ_HOLDING_REGISTERS
        /// </summary>
        public const byte FC_READ_HOLDING_REGISTERS = 0x03;
        /// <summary>
        /// FC_WRITE_SINGLE_COIL
        /// </summary>
        public const byte FC_WRITE_SINGLE_COIL = 0x05;
        /// <summary>
        /// FC_WRITE_SINGLE_REGISTER
        /// </summary>
        public const byte FC_WRITE_SINGLE_REGISTER = 0x06;
        /// <summary>
        /// FC_WRITE_MULTIPLE_REGISTERS
        /// </summary>
        public const byte FC_WRITE_MULTIPLE_REGISTERS = 0x10;

        private TcpClient _client;
        private readonly object _lock = new();
        private ushort _transactionId;
        private string _lastIP;
        private int _lastPort;
        private readonly BackgroundWorker _connectWorker;

        /// <summary>
        /// UnitId
        /// </summary>
        public byte UnitId { get; set; } = 1;

        /// <summary>
        /// ModbusTcp客户端
        /// </summary>
        public ModbusTcpClient()
        {
            _connectWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            _connectWorker.DoWork += ConnectLoop;
            if (!_connectWorker.IsBusy)
                _connectWorker.RunWorkerAsync();
        }

        private void ConnectLoop(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                try
                {
                    // 定期 ping 更新 Online
                    Online = NetTool.PingIP(IP_SerialPort);

                    // IP/Port 变更 → 断连
                    if (IP_SerialPort != _lastIP || Port != _lastPort)
                    {
                        _client?.Close();
                        _client = null;
                        Connected = false;
                        _lastIP = IP_SerialPort;
                        _lastPort = Port;
                    }

                    // 在线且未连接 → 自动连接
                    if (Online && !Connected && !string.IsNullOrEmpty(IP_SerialPort))
                    {
                        if (DoConnect())
                        {
                            Connected = true;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch { }
                Thread.Sleep(500);
            }
        }

        private bool DoConnect()
        {
            if (string.IsNullOrEmpty(IP_SerialPort)) return false;
            if (!IPAddress.TryParse(IP_SerialPort, out _)) return false;

            _client?.Close();
            _client = new TcpClient();

            try
            {
                _client.Connect(IP_SerialPort, Port);
                if (_client.Connected)
                {
                    Log($"{DateTime.Now} ;{IP_SerialPort}:{Port} 连接成功");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"{IP_SerialPort}:{Port} 无法连接: {ex.Message}");
            }
            return false;
        }

        /// <summary>获取网络流</summary>
        public NetworkStream GetStream()
        {
            return _client?.GetStream();
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public override void Close()
        {
            _connectWorker.CancelAsync();
            _connectWorker.Dispose();
            _client?.Close();
            _client = null;
            Connected = false;
        }

        // ──────── Modbus 协议 ────────

        private byte[] BuildRequest(byte unitId, byte functionCode, byte[] data)
        {
            _transactionId++;
            ushort length = (ushort)(2 + 1 + data.Length);
            using MemoryStream ms = new();
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)_transactionId)), 0, 2);
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)0)), 0, 2);
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)length)), 0, 2);
            ms.WriteByte(unitId);
            ms.WriteByte(functionCode);
            ms.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        private static bool ReadAll(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = stream.Read(buffer, offset + total, count - total);
                if (read <= 0) return false;
                total += read;
            }
            return true;
        }

        private byte[] Execute(byte unitId, byte functionCode, byte[] requestData)
        {
            lock (_lock)
            {
                if (!Connected) return null;
                var stream = GetStream();
                if (stream == null) return null;

                try
                {
                    byte[] request = BuildRequest(unitId, functionCode, requestData);
                    stream.Write(request, 0, request.Length);
                    Log($"{DateTime.Now:HH:mm:ss:fff} ModbusTx [{BitConverter.ToString(request).Replace("-", " ")}]");

                    stream.ReadTimeout = 5000;

                    byte[] mbap = new byte[7];
                    if (!ReadAll(stream, mbap, 0, 7)) { Log("ModbusTCP: 读MBAP失败"); return null; }

                    ushort respLen = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(mbap, 4));
                    byte[] pdu = new byte[respLen - 1];
                    if (!ReadAll(stream, pdu, 0, pdu.Length)) { Log("ModbusTCP: 读PDU失败"); return null; }

                    Log($"ModbusRx [{BitConverter.ToString(mbap).Replace("-", " ")} {BitConverter.ToString(pdu).Replace("-", " ")}]");

                    byte respFunc = pdu[0];
                    if ((respFunc & 0x80) != 0)
                    {
                        Log($"ModbusTCP 异常: func=0x{respFunc & 0x7F:X2} code={(pdu.Length > 1 ? pdu[1] : 0)}");
                        return null;
                    }

                    Successful = true;
                    ReceiveData = [.. mbap, .. pdu];
                    return pdu.Length > 1 ? pdu[1..] : [];
                }
                catch (Exception ex)
                {
                    Log($"ModbusTCP 异常: {ex.Message}");
                    Connected = false;
                    return null;
                }
            }
        }

        // ─── 公开 API ───

        /// <summary>
        /// 读取HoldingRegisters
        /// </summary>
        /// <param name="startAddress">启动Address</param>
        /// <param name="count">数量</param>
        /// <param name="unitId">unitId</param>
        /// <returns>返回结果</returns>
        public ushort[] ReadHoldingRegisters(ushort startAddress, ushort count, byte? unitId = null)
        {
            byte uid = unitId ?? UnitId;
            byte[] data = new byte[4];
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)startAddress)), 0, data, 0, 2);
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)count)), 0, data, 2, 2);

            byte[] resp = Execute(uid, FC_READ_HOLDING_REGISTERS, data);
            if (resp == null || resp.Length == 0) return null;

            ushort[] registers = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                int offset = 1 + i * 2;
                if (offset + 1 < resp.Length)
                    registers[i] = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(resp, offset));
            }
            return registers;
        }

        /// <summary>
        /// 写入SingleCoil
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">值</param>
        /// <param name="unitId">unitId</param>
        /// <returns>返回布尔值</returns>
        public bool WriteSingleCoil(ushort address, bool value, byte? unitId = null)
        {
            byte uid = unitId ?? UnitId;
            byte[] data = new byte[4];
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)address)), 0, data, 0, 2);
            short coilVal = value ? unchecked((short)0xFF00) : (short)0x0000;
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(coilVal)), 0, data, 2, 2);
            return Execute(uid, FC_WRITE_SINGLE_COIL, data) != null;
        }

        /// <summary>
        /// 写入Single注册
        /// </summary>
        /// <param name="address">address</param>
        /// <param name="value">值</param>
        /// <param name="unitId">unitId</param>
        /// <returns>返回布尔值</returns>
        public bool WriteSingleRegister(ushort address, ushort value, byte? unitId = null)
        {
            byte uid = unitId ?? UnitId;
            byte[] data = new byte[4];
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)address)), 0, data, 0, 2);
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)value)), 0, data, 2, 2);
            return Execute(uid, FC_WRITE_SINGLE_REGISTER, data) != null;
        }

        /// <summary>
        /// 写入MultipleRegisters
        /// </summary>
        /// <param name="startAddress">启动Address</param>
        /// <param name="values">values</param>
        /// <param name="unitId">unitId</param>
        /// <returns>返回布尔值</returns>
        public bool WriteMultipleRegisters(ushort startAddress, ushort[] values, byte? unitId = null)
        {
            byte uid = unitId ?? UnitId;
            int byteCount = values.Length * 2;
            byte[] data = new byte[5 + byteCount];
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)startAddress)), 0, data, 0, 2);
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)values.Length)), 0, data, 2, 2);
            data[4] = (byte)byteCount;
            for (int i = 0; i < values.Length; i++)
                Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)values[i])), 0, data, 5 + i * 2, 2);
            return Execute(uid, FC_WRITE_MULTIPLE_REGISTERS, data) != null;
        }
    }
}
