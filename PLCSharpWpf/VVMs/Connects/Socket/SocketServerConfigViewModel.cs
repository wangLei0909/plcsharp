using DryIoc;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.Core.Tools;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace PLCSharp.VVMs.Connects.Socket
{

    /// <summary>
    /// Socket服务端配置视图模型
    /// </summary>
    public class SocketServerConfigViewModel : ValidateBase, IDialogAware
    {
        /// <summary>
        /// Socket服务端配置视图模型
        /// </summary>
        public SocketServerConfigViewModel(IContainerExtension container)
        {
            _ConnectsModel = container.Resolve<ConnectsModel>();
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();
        }

        private readonly ConnectsModel _ConnectsModel;
        private string _Title = "Socket Server";

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get { return _Title; }
            set
            {
                SetProperty(ref _Title, value);
            }
        }

        /// <summary>
        /// Request关闭
        /// </summary>
        public DialogCloseListener RequestClose { get; }

        /// <summary>
        /// Can关闭对话框
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool CanCloseDialog()
        {
            return true;
        }

        /// <summary>
        /// 关闭对话框后要执行的
        /// </summary>
        public void OnDialogClosed()
        {
            Server.LogSwitch = false;
            bkgWorker.CancelAsync();
            bkgWorker.Dispose();
        }

        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            var name = parameters.GetValue<string>("Name");

            Server = _ConnectsModel.Connects.Where(c => c.Name == name).FirstOrDefault();

            IP = Server.IP_SerialPort;
            Port = Server.Port;
            Server.LogSwitch = true;


        }

        private readonly BackgroundWorker bkgWorker;

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                Thread.Sleep(10);
                if (Server != null)
                {

                    for (int i = 0; i < Server.Clients.Count; i++)
                    {
                        Server.Clients[i].Online = NetTool.PingIP(Server.Clients[i].IP_SerialPort);

                    }
                    if (Server.LogSwitch)
                    {
                        if (!(Server as SocketServer).LogQueue.IsEmpty)
                        {
                            if ((Server as SocketServer).LogQueue.TryDequeue(out string log))

                                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    Logs.Add(new(log));
                                }));
                        }
                    }
                }

            }
        }


        private Connect _Server;
        /// <summary>
        /// 配置项
        /// </summary>
        public Connect Server
        {
            get { return _Server; }
            set { SetProperty(ref _Server, value); }
        }

        private Connect _SelectedClient;
        /// <summary>
        /// 配置项
        /// </summary>
        public Connect SelectedClient
        {
            get { return _SelectedClient; }
            set
            {
                SetProperty(ref _SelectedClient, value);
                if (value != null)
                {
                    Server.Params.DataType = value.Params.DataType;

                }

            }
        }
        //日志
        private ObservableCollection<ErrorLog> _Logs = [];

        /// <summary>
        /// Logs
        /// </summary>
        public ObservableCollection<ErrorLog> Logs
        {
            get { return _Logs; }
            set { SetProperty(ref _Logs, value); }
        }

        // IP
        private string _IP = "127.0.0.1";

        /// <summary>
        /// IP
        /// </summary>
        [Required(ErrorMessage = "IP不能为空！")]
        [RegularExpression(@"^([1-9]\d?|1\d{2}|2[01]\d|22[0-3])(\.([1-9]?\d|1\d{2}|2[0-4]\d|25[0-5])){3}$", ErrorMessage = "IP地址格式不正确")]
        public string IP
        {
            get { return _IP; }
            set
            {
                SetProperty(ref _IP, value);
                Server.IP_SerialPort = value;
            }
        }

        // PORT
        private int _Port = 7950;

        /// <summary>
        /// 端口
        /// </summary>
        [Required(ErrorMessage = "端口不能为空！")]
        [Range(0, 65535, ErrorMessage = "端口应在0-65535之间.")]
        public int Port
        {
            get { return _Port; }
            set
            {
                SetProperty(ref _Port, value);

                Server.Port = value;
            }
        }


        private string _SendString = "hello";

        /// <summary>
        /// 发送String
        /// </summary>
        public string SendString
        {
            get { return _SendString; }
            set
            {
                SetProperty(ref _SendString, value);
            }
        }

        #region Command

        private AsyncDelegateCommand _Send;

        /// <summary>
        /// 发送
        /// </summary>
        public AsyncDelegateCommand Send =>
            _Send ??= new AsyncDelegateCommand(ExecuteSendAsync);


        private async Task ExecuteSendAsync()

        {
            if (SelectedClient == null)
            {

                Server.Log("请选择客户端"); return;

            }

            await Server.SendAsync(SendString, SelectedClient.Name);
            if (Server.Params.DataType == CommunicationDataType.Bytes)
            {

                SendString = Server.SendInfo;

            }

        }
        private AsyncDelegateCommand _SendToAll;
        /// <summary>
        /// 发送ToAll
        /// </summary>
        public AsyncDelegateCommand SendToAll =>
            _SendToAll ??= new AsyncDelegateCommand(ExecuteSendToAllAsync);

        private async Task ExecuteSendToAllAsync()
        {
            await Server.SendAsync(SendString);
            if (Server.Params.DataType == CommunicationDataType.Bytes)
            {
                SendString = Server.SendInfo;
            }
        }
        private DelegateCommand<string> _ConnectsManage;

        /// <summary>
        /// Connects管理
        /// </summary>
        public DelegateCommand<string> ConnectsManage =>
            _ConnectsManage ??= new DelegateCommand<string>(ExecuteConnectsManage);

        private void ExecuteConnectsManage(string cmd)
        {
            switch (cmd)
            {
                case "New":
                    Server.Clients.Add(new Connect()
                    {
                        Name = Guid.NewGuid().ToString().Substring(1, 5),
                        IP_SerialPort = "127.0.0.1",
                        Comment = "新连接",
                    });
                    _ConnectsModel.SaveConnects();

                    break;

                case "Save":
                    SaveConnects();
                    break;

                case "Remove":
                    if (SelectedClient != null)
                    {
                        Server.Clients.Remove(SelectedClient);
                        SelectedClient = null;
                        _ConnectsModel.SaveConnects();
                    }
                    break;
            }
        }



        private void SaveConnects()
        {
            var names = new List<string>();

            foreach (var item in Server.Clients)
            {
                if (string.IsNullOrEmpty(item.Name))
                {
                    _ConnectsModel.SendInfoDialog($"保存失败，名称{item.Name}不合适！");
                    return;
                }

                if (names.Contains(item.Name))
                {
                    _ConnectsModel.SendInfoDialog($"保存失败，重复的名称{item.Name}！" );
                    return;
                }
                else
                {
                    names.Add(item.Name);
                }
            }


            SelectedClient.Params.Prompt = "";
            _ConnectsModel.SaveConnects();
        }

        #endregion Command
    }
}