using PLCSharp.Core.Prism;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;


namespace PLCSharp.VVMs.Connects
{
    /// <summary>
    /// 新建连接视图模型
    /// </summary>
    public class NewConnectViewModel : DialogAwareBase
    {

        private string _Name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set
            {
                SetProperty(ref _Name, value);
            }
        }
        private ProtocolType _Type;
        /// <summary>
        /// 类型
        /// </summary>
        public ProtocolType Type
        {
            get { return _Type; }
            set
            {
                SetProperty(ref _Type, value);

                switch (value)
                {
                    case ProtocolType.SocketClient:
                        IP_SerialPort = "192.168.0.1";
                        break;
                    case ProtocolType.SocketSever:
                        IP_SerialPort = "192.168.0.1";
                        break;
                    case ProtocolType.ModbusTcpClient:
                        IP_SerialPort = "192.168.0.1";
                        break;
                    case ProtocolType.ModbusTcpServer:
                        IP_SerialPort = "192.168.0.1";
                        break;
                    case ProtocolType.FreeSerialProtocol:
                        IP_SerialPort = "COM1";
                        break;
                    case ProtocolType.ModbusRtuClient:
                        IP_SerialPort = "COM1";
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 新建连接视图模型
        /// </summary>
        public NewConnectViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
        }

        private string _IP_SerialPort;
        /// <summary>
        /// IP_串口端口
        /// </summary>
        public string IP_SerialPort
        {
            get { return _IP_SerialPort; }
            set { SetProperty(ref _IP_SerialPort, value); }
        }
        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            Name = Guid.NewGuid().ToString().Substring(1, 5);
            Type = ProtocolType.SocketClient;
            Title = "New Connect";
        }
        /// <summary>
        /// RaiseRequest关闭
        /// </summary>
        /// <param name="dialogResult">对话框结果</param>
        public override void RaiseRequestClose(IDialogResult dialogResult)
        {
            dialogResult.Parameters.Add("Name", Name);
            dialogResult.Parameters.Add("Type", Type);
            dialogResult.Parameters.Add("IP_SerialPort", IP_SerialPort);
            base.RaiseRequestClose(dialogResult);
        }
    }
}
