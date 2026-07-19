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

namespace PLCSharp.VVMs.Connects.ModbusTcp
{
    /// <summary>
    /// Modbus客户端配置视图模型
    /// </summary>
    public class ModbusClientConfigViewModel : ValidateBase, IDialogAware
    {
        /// <summary>
        /// Modbus客户端配置视图模型
        /// </summary>
        public ModbusClientConfigViewModel(IContainerExtension container)
        {
            _ConnectsModel = container.Resolve<ConnectsModel>();
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();
        }

        private readonly ConnectsModel _ConnectsModel;
        private string _Title = "Modbus TCP 客户端";

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get => _Title;
            set => SetProperty(ref _Title, value);
        }

        /// <summary>
        /// Request关闭
        /// </summary>
        public DialogCloseListener RequestClose { get; }

        /// <summary>
        /// Can关闭对话框
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool CanCloseDialog() => true;

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
            Client = _ConnectsModel.Connects.FirstOrDefault(c => c.Name == name);
            if (Client == null) return;

            IP = Client.IP_SerialPort;
            Port = Client.Port;
            Client.LogSwitch = true;

        }

        private readonly BackgroundWorker bkgWorker;

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                Thread.Sleep(100);
                if (Client == null) continue;

                // 日志轮询
                if (Client.LogSwitch && !Client.LogQueue.IsEmpty)
                {
                    if (Client.LogQueue.TryDequeue(out string log))
                    {
                        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(new Action(() =>
                        {
                            Logs.Add(new ErrorLog(log));
                        }));
                    }
                }
            }
        }


        #region Properties

        private Connect _Client;
        /// <summary>
        /// 客户端
        /// </summary>
        public Connect Client
        {
            get => _Client;
            set => SetProperty(ref _Client, value);
        }

        private ObservableCollection<ErrorLog> _Logs = [];
        /// <summary>
        /// Logs
        /// </summary>
        public ObservableCollection<ErrorLog> Logs
        {
            get => _Logs;
            set => SetProperty(ref _Logs, value);
        }

        private string _IP = "127.0.0.1";
        /// <summary>
        /// IP
        /// </summary>
        [Required(ErrorMessage = "IP不能为空！")]
        [RegularExpression(@"^([1-9]\d?|1\d{2}|2[01]\d|22[0-3])(\.([1-9]?\d|1\d{2}|2[0-4]\d|25[0-5])){3}$", ErrorMessage = "IP地址格式不正确")]
        public string IP
        {
            get => _IP;
            set
            {
                SetProperty(ref _IP, value);
                if (Client != null) Client.IP_SerialPort = value;
            }
        }

        private int _Port = 502;
        /// <summary>
        /// 端口
        /// </summary>
        [Required(ErrorMessage = "端口不能为空！")]
        [Range(0, 65535, ErrorMessage = "端口应在0-65535之间.")]
        public int Port
        {
            get => _Port;
            set
            {
                SetProperty(ref _Port, value);
                if (Client != null) Client.Port = value;
            }
        }

        private DataItem _SelectedDataItem;
        /// <summary>
        /// Selected数据项
        /// </summary>
        public DataItem SelectedDataItem
        {
            get => _SelectedDataItem;
            set => SetProperty(ref _SelectedDataItem, value);
        }

        #endregion

        #region Commands

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
                    ushort nextAddr = 0;
                    while (Client.DataItems.Any(i => i.Address == nextAddr))
                        nextAddr++;
                    Client.DataItems.Add(new DataItem
                    {
                        Address = nextAddr,
                        Value = 0,
                        Description = "",
                    });
                    break;

                case "Remove":
                    if (SelectedDataItem != null)
                        Client.DataItems.Remove(SelectedDataItem);
                    break;

                case "Send":
                    if (SelectedDataItem != null)
                    {
                        Client.Log($"手动发送: HR @ {SelectedDataItem.Address}");
                        _ConnectsModel.ApplyDataItemToServer(Client.Name, SelectedDataItem);
                    }
                    break;

                case "SendAll":
                    Client.Log("发送全部数据项");
                    _ConnectsModel.ApplyAllDataToServer(Client.Name);
                    break;

                case "Save":
                    _ConnectsModel.SaveConnects();
                    Client.Log("地址表已保存");
                    break;
            }
        }

        #endregion
    }
}
