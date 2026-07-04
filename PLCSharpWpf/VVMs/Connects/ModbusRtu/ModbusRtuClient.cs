using System.IO;
using System.Net;
using System.Threading;

namespace PLCSharp.VVMs.Connects.ModbusRtu
{
    /// <summary>
    /// Modbus RTU 客户端 — 串口通信，继承 Connect
    /// </summary>
    public class ModbusRtuClient : Connect
    {
        /// <summary>
        /// FC_READ_HOLDING_REGISTERS
        /// </summary>
        public const byte FC_READ_HOLDING_REGISTERS = 0x03;
        /// <summary>
        /// FC_WRITE_SINGLE_REGISTER
        /// </summary>
        public const byte FC_WRITE_SINGLE_REGISTER = 0x06;
        /// <summary>
        /// FC_WRITE_MULTIPLE_REGISTERS
        /// </summary>
        public const byte FC_WRITE_MULTIPLE_REGISTERS = 0x10;

        private readonly object _lock = new();

        private System.IO.Ports.SerialPort _port;



        /// <summary>打开串口</summary>
        public bool Open()
        {
            try
            {
                _port?.Close();
                _port = new System.IO.Ports.SerialPort(IP_SerialPort, Params.BaudRate, Params.ParityValue, Params.DataBits, Params.StopBitsValue)
                {
                    ReadTimeout = 5000,
                    WriteTimeout = 2000
                };
                _port.Open();
                Connected = true;
                Log($"串口 {IP_SerialPort} 已打开");
                return true;
            }
            catch (Exception ex)
            {
                Log($"打开串口失败: {ex.Message}");
                Connected = false;
                return false;
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public override void Close()
        {
            try { _port?.Close(); } catch { }
            _port = null;
            Connected = false;
        }

        // ─── CRC16-Modbus ───

        private static ushort CalcCrc(byte[] data, int offset, int count)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < count; i++)
            {
                crc ^= data[offset + i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }
            return crc;
        }

        // ─── 构建 RTU 帧 ───

        private static byte[] BuildRequest(byte unitId, byte functionCode, byte[] data)
        {
            byte[] frame = new byte[1 + 1 + data.Length + 2];
            frame[0] = unitId;
            frame[1] = functionCode;
            Array.Copy(data, 0, frame, 2, data.Length);
            ushort crc = CalcCrc(frame, 0, frame.Length - 2);
            frame[^2] = (byte)(crc & 0xFF);
            frame[^1] = (byte)((crc >> 8) & 0xFF);
            return frame;
        }

        // ─── 读写串口 ───

        private static bool ReadAll(System.IO.Ports.SerialPort port, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                try
                {
                    int read = port.Read(buffer, offset + total, count - total);
                    if (read <= 0) return false;
                    total += read;
                }
                catch { return false; }
            }
            return true;
        }

        private byte[] Execute(byte unitId, byte functionCode, byte[] requestData)
        {
            lock (_lock)
            {
                if (_port == null || !_port.IsOpen)
                {
                    Log("ModbusRTU: 串口未打开");
                    return null;
                }

                try
                {
                    // 清空接收缓冲区，避免残留数据污染本次读取
                    _port.DiscardInBuffer();

                    byte[] request = BuildRequest(unitId, functionCode, requestData);
                    _port.Write(request, 0, request.Length);
                    Log($"ModbusTx [{BitConverter.ToString(request).Replace("-", " ")}]");

                    // 等待响应：按波特率计算 3.5 字符时间 + 50ms 从机处理余量
                    double charMs = 1000.0 / Params.BaudRate * 10; // 8N1 = 10 bits/char
                    int waitMs = (int)Math.Ceiling(3.5 * charMs) + 50;
                    if (waitMs < 20) waitMs = 20;
                    Thread.Sleep(waitMs);

                    // 读响应：先读头 2 字节（地址 + 功能码），再读后续
                    byte[] head = new byte[2];
                    if (!ReadAll(_port, head, 0, 2))
                    {
                        Log("ModbusRTU: 读响应头失败");
                        _port.DiscardInBuffer();
                        return null;
                    }

                    byte respFunc = head[1];
                    bool isError = (respFunc & 0x80) != 0;

                    // 确定 PDU 长度
                    int dataLen;
                    if (functionCode == FC_READ_HOLDING_REGISTERS && !isError)
                    {
                        // 读寄存器：功能码后跟字节数
                        byte[] bc = new byte[1];
                        if (!ReadAll(_port, bc, 0, 1))
                        {
                            _port.DiscardInBuffer();
                            return null;
                        }
                        dataLen = bc[0]; // 功能码 + 数据字节数
                    }
                    else if (!isError)
                    {
                        // 写操作：地址+功能码+数据固定 4 字节
                        dataLen = 4;
                    }
                    else
                    {
                        // 异常响应：地址+异常码共 1 字节数据
                        dataLen = 1;
                    }

                    // 读剩余 PDU 数据
                    byte[] pduExtra = new byte[dataLen];
                    if (!ReadAll(_port, pduExtra, 0, dataLen))
                    {
                        Log("ModbusRTU: 读 PDU 失败");
                        _port.DiscardInBuffer();
                        return null;
                    }

                    // 组合完整响应（不含 CRC）
                    byte[] response = [head[0], head[1], (byte)dataLen, .. pduExtra];

                    // 读 CRC（2 字节）
                    byte[] crcBytes = new byte[2];
                    if (!ReadAll(_port, crcBytes, 0, 2))
                    {
                        Log("ModbusRTU: 读 CRC 失败");
                        _port.DiscardInBuffer();
                        return null;
                    }

                    // 验证 CRC
                    ushort expectedCrc = CalcCrc(response, 0, response.Length);
                    ushort actualCrc = (ushort)(crcBytes[0] | (crcBytes[1] << 8));
                    if (expectedCrc != actualCrc)
                    {
                        Log($"ModbusRTU: CRC 校验失败 (期望={expectedCrc:X4}, 实际={actualCrc:X4})");
                        _port.DiscardInBuffer();
                        return null;
                    }

                    Log($"ModbusRx [{BitConverter.ToString(response).Replace("-", " ")}]");

                    if (isError)
                    {
                        byte code = pduExtra.Length > 0 ? pduExtra[0] : (byte)0;
                        Log($"ModbusRTU 异常: func=0x{respFunc & 0x7F:X2} code={code}");
                        return null;
                    }

                    Successful = true;
                    ReceiveData = response;
                    return response;
                }
                catch (TimeoutException)
                {
                    Log("ModbusRTU: 读取超时");
                    _port?.DiscardInBuffer();
                    return null;
                }
                catch (IOException)
                {
                    Log("ModbusRTU: IO 错误");
                    _port?.DiscardInBuffer();
                    return null;
                }
                catch (Exception ex)
                {
                    Log($"ModbusRTU 异常: {ex.Message}");
                    _port?.DiscardInBuffer();
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
            byte uid = unitId ?? Params.UnitId;
            byte[] data = new byte[4];
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)startAddress)), 0, data, 0, 2);
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)count)), 0, data, 2, 2);

            byte[] response = Execute(uid, FC_READ_HOLDING_REGISTERS, data);
            if (response == null || response.Length < 3) return null;

            // response = [addr, FC, byteCount, data...]
            byte byteCount = response[2];
            ushort[] registers = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                int offset = 3 + i * 2;
                if (offset + 1 < response.Length)
                    registers[i] = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, offset));
            }
            return registers;
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
            byte uid = unitId ?? Params.UnitId;
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
            byte uid = unitId ?? Params.UnitId;
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
