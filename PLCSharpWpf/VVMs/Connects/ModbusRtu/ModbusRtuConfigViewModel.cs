using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.VVMs.Connects.ModbusTcp;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace PLCSharp.VVMs.Connects.ModbusRtu
{
    /// <summary>
    /// ModbusRtu配置视图模型
    /// </summary>
    public class ModbusRtuConfigViewModel : ValidateBase, IDialogAware
    {
        private readonly ConnectsModel _ConnectsModel;
        private ModbusRtuClient _Client;
        /// <summary>
        /// 客户端
        /// </summary>
        public ModbusRtuClient Client
        {
            get { return _Client; }
            set { SetProperty(ref _Client, value); }
        }

        /// <summary>
        /// ModbusRtu配置视图模型
        /// </summary>
        public ModbusRtuConfigViewModel(IContainerExtension container)
        {
            _ConnectsModel = container.Resolve<ConnectsModel>();
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();
        }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title => "Modbus RTU 客户端";

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

        }

        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            var name = parameters.GetValue<string>("Name");
            Client = _ConnectsModel.Connects.FirstOrDefault(c => c.Name == name) as ModbusRtuClient;
            if (Client == null) return;

            Client.LogSwitch = true;

        }



        // ─── 串口操作 ───
        private DelegateCommand _OpenPortCommand;
        /// <summary>
        /// 打开端口Command
        /// </summary>
        public DelegateCommand OpenPortCommand =>
            _OpenPortCommand ??= new DelegateCommand(() => Client?.Open());

        private DelegateCommand _ClosePortCommand;
        /// <summary>
        /// 关闭端口Command
        /// </summary>
        public DelegateCommand ClosePortCommand =>
            _ClosePortCommand ??= new DelegateCommand(() => Client?.Close());

        // ─── DataItems 管理 ───
        private DelegateCommand<string> _ConnectsManage;
        /// <summary>
        /// Connects管理
        /// </summary>
        public DelegateCommand<string> ConnectsManage =>
            _ConnectsManage ??= new DelegateCommand<string>(ExecuteConnectsManage);

        private DataItem _SelectedDataItem;
        /// <summary>
        /// Selected数据项
        /// </summary>
        public DataItem SelectedDataItem
        {
            get => _SelectedDataItem;
            set => SetProperty(ref _SelectedDataItem, value);
        }

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
                    break;
            }
        }

        // ─── 日志 ───
        private ObservableCollection<ErrorLog> _Logs = [];
        /// <summary>
        /// Logs
        /// </summary>
        public ObservableCollection<ErrorLog> Logs
        {
            get => _Logs;
            set => SetProperty(ref _Logs, value);
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
                        System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            Logs.Add(new ErrorLog(log));
                        }));
                    }
                }
            }
        }

    }
}
