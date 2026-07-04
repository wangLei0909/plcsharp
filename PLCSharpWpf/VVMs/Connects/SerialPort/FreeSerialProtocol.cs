using System.IO.Ports;
using System.Text;

namespace PLCSharp.VVMs.Connects.SerialPort
{
    /// <summary>
    /// Free串口协议
    /// </summary>
    class FreeSerialProtocol : Connect
    {
        /// <summary>
        /// my串口
        /// </summary>
        public System.IO.Ports.SerialPort mySerial;

        public event Action<byte[]> ReceiveBytes;

        readonly byte[] readBuffer = new byte[1024];
        private void DataReceivedHandlerScan(object sender, SerialDataReceivedEventArgs e)
        {

            var spReceive = (System.IO.Ports.SerialPort)sender;
            var count = Math.Min(spReceive.BytesToRead, readBuffer.Length);
            spReceive.Read(readBuffer, 0, count);
            var receiveBuffer = readBuffer[0..count];
            ReceiveBytes?.Invoke(receiveBuffer);
            if (Params.DataType == CommunicationDataType.Bytes)
            {
                var hexString = Convert.ToHexString(receiveBuffer);
                if (Connect.HexStringToBytes(hexString, out List<byte> bytes, out string fomat))
                {
                    Log("接收 <==   " + fomat);
                }
            }
            else
            {
                Log("接收 <==   " + Encoding.UTF8.GetString(receiveBuffer, 0, receiveBuffer.Length));
            }
        }
        /// <summary>
        /// 打开
        /// </summary>
        public override void Open()
        {
            Close();
            mySerial = new()
            {
                PortName = IP_SerialPort,
                BaudRate = Params.BaudRate,
                Parity = Params.ParityValue,
                StopBits = Params.StopBitsValue,
                DataBits = Params.DataBits,
                Handshake = Handshake.None,
                WriteTimeout = 100
            };
            mySerial.Open();
            mySerial.DataReceived += DataReceivedHandlerScan;
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public override void Close()
        {
            if (mySerial != null)
            {
                mySerial.DataReceived -= DataReceivedHandlerScan;
                mySerial.Close();
            }
        }

        /// <summary>
        /// 发送数据Async
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <param name="isLog">is日志</param>
        /// <returns>返回结果</returns>
        public override async Task<bool> SendDataAsync(byte[] bytes, bool isLog = true)
        {

            return await Task<bool>.Run(() =>
            {
                if (bytes == null || bytes.Length == 0) return false;
                if (mySerial != null && mySerial.IsOpen)
                {
                    try
                    {
                        mySerial.Write(bytes, 0, bytes.Length);
                        if (isLog)
                        {
                            var bytesStr = BitConverter.ToString(bytes).Replace("-", " ");
                            Log($"发送 ==>   {bytesStr}");
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

            });




        }
    }
}
