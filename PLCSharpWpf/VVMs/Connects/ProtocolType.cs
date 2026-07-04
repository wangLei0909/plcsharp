namespace PLCSharp.VVMs.Connects
{
    public enum ProtocolType
    {
        Undefined = 0,
        SocketClient = 1,
        SocketSever = 2,
        ModbusTcpClient = 3,
        ModbusTcpServer = 4,
        FreeSerialProtocol,
        ModbusRtuClient,
    }
}