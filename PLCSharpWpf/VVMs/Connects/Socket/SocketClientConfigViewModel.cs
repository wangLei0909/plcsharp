using DryIoc;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
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
    /// Socket客户端配置视图模型
    /// </summary>
    public class SocketClientConfigViewModel : ValidateBase, IDialogAware
    {
        /// <summary>
        /// Socket客户端配置视图模型
        /// </summary>
        public SocketClientConfigViewModel(IContainerExtension container)
        {
            _ConnectsModel = container.Resolve<ConnectsModel>();
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();
        }

        private readonly ConnectsModel _ConnectsModel;
        private string _Title = "Socket Client";

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
            Client.LogSwitch = false;
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

            Client = _ConnectsModel.Connects.Where(c => c.Name == name).FirstOrDefault();

            IP = Client.IP_SerialPort;
            Port = Client.Port;
            Client.LogSwitch = true;


        }

        private readonly BackgroundWorker bkgWorker;

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                Thread.Sleep(100);
                if (Client != null)
                {

                    if (Client.LogSwitch)
                    {
                        if (!(Client as SocketClient).LogQueue.IsEmpty)
                        {
                            if ((Client as SocketClient).LogQueue.TryDequeue(out string log))

                                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    Logs.Add(new ErrorLog(log));
                                }));
                        }
                    }
                }

            }
        }




        private Connect _Client;
        /// <summary>
        /// 配置项
        /// </summary>
        public Connect Client
        {
            get { return _Client; }
            set { SetProperty(ref _Client, value); }
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
                Client.IP_SerialPort = value;
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

                Client.Port = value;
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
            await Client.SendAsync(SendString);

        }



        #endregion Command
    }
}