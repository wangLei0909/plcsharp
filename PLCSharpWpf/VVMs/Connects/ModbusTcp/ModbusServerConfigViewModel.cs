using DryIoc;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace PLCSharp.VVMs.Connects.ModbusTcp
{
    /// <summary>
    /// Modbus服务端配置视图模型
    /// </summary>
    public class ModbusServerConfigViewModel : ValidateBase, IDialogAware
    {
        /// <summary>
        /// Modbus服务端配置视图模型
        /// </summary>
        public ModbusServerConfigViewModel(IContainerExtension container)
        {
            _ConnectsModel = container.Resolve<ConnectsModel>();
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();
        }

        private readonly ConnectsModel _ConnectsModel;
        private string _Title = "Modbus TCP 服务端";
        private bool _hasSubscribedCollection;

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
            // 取消远程写入监听
            if (Server is ModbusTcpServer modbusSrv)
                modbusSrv.DataWritten -= OnServerDataWritten;

            Server.LogSwitch = false;
            bkgWorker.CancelAsync();
            bkgWorker.Dispose();
            if (Server?.DataItems is INotifyCollectionChanged nc)
                nc.CollectionChanged -= OnDataItemsChanged;
            // 取消所有条目的属性监听
            if (Server?.DataItems != null)
            {
                foreach (var item in Server.DataItems)
                    item.PropertyChanged -= OnDataItemPropertyChanged;
            }
        }

        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            var name = parameters.GetValue<string>("Name");
            Server = _ConnectsModel.Connects.FirstOrDefault(c => c.Name == name);
            if (Server == null) return;

            IP = Server.IP_SerialPort;
            Port = Server.Port;
            Server.LogSwitch = true;

            // 监听 DataItems 变化，实时同步到服务端（仅当数据项变化时，而非轮询）
            SubscribeDataItemsChanges();
            HookPropertyChangedOnAllItems();

            // 监听远程客户端写入，更新 DataItem 表格显示
            if (Server is ModbusTcpServer modbusSrv)
                modbusSrv.DataWritten += OnServerDataWritten;
        }

        private void OnServerDataWritten(ushort address, ushort value)
        {
            if (Server?.DataItems == null) return;

            // 查找匹配的 DataItem
            DataItem item = null;
            foreach (var di in Server.DataItems)
            {
                if (di.Address == address)
                {
                    item = di; break;
                }
                // Int32/Float 占用2个寄存器，String 占2个，写后续地址时匹配到起始条目
                if (address > di.Address &&
                    (di.ValueInterpretation == DataItem.ValueInterpretationEnum.Int32 ||
                     di.ValueInterpretation == DataItem.ValueInterpretationEnum.Float ||
                     di.ValueInterpretation == DataItem.ValueInterpretationEnum.String))
                {
                    int span = address - di.Address;
                    bool inRange = di.ValueInterpretation switch
                    {
                        DataItem.ValueInterpretationEnum.Int32 or DataItem.ValueInterpretationEnum.Float => span == 1,
                        DataItem.ValueInterpretationEnum.String => span < 2,
                        _ => false,
                    };
                    if (inRange) { item = di; break; }
                }
            }
            if (item == null) return;

            // 读取当前服务器值更新 DataItem
            if (Server is ModbusTcpServer srv)
            {
                UpdateDataItemFromServer(srv, item);
            }
        }

        private void UpdateDataItemFromServer(ModbusTcpServer srv, DataItem item)
        {
            ushort lo = srv.GetHoldingRegister(item.Address);
            ushort hi = (item.ValueInterpretation == DataItem.ValueInterpretationEnum.Int32 ||
                         item.ValueInterpretation == DataItem.ValueInterpretationEnum.Float ||
                         item.ValueInterpretation == DataItem.ValueInterpretationEnum.String)
                        ? srv.GetHoldingRegister((ushort)(item.Address + 1))
                        : (ushort)0;

            item.SetRawValues(lo, hi);
        }

        private void SubscribeDataItemsChanges()
        {
            if (_hasSubscribedCollection || Server?.DataItems == null) return;
            if (Server.DataItems is INotifyCollectionChanged nc)
            {
                nc.CollectionChanged += OnDataItemsChanged;
                _hasSubscribedCollection = true;
            }
        }

        private void OnDataItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // 新加的条目要挂载属性变更监听
            if (e.NewItems != null)
            {
                foreach (DataItem item in e.NewItems)
                {
                    item.PropertyChanged -= OnDataItemPropertyChanged;
                    item.PropertyChanged += OnDataItemPropertyChanged;
                }
            }
            // 移除的条目要取消监听
            if (e.OldItems != null)
            {
                foreach (DataItem item in e.OldItems)
                {
                    item.PropertyChanged -= OnDataItemPropertyChanged;
                }
            }

            if (Server is ModbusTcpServer modbusServer)
            {
                _ConnectsModel.ApplyAllDataToServer(modbusServer);

            }
        }

        private readonly BackgroundWorker bkgWorker;

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                Thread.Sleep(100);
                if (Server != null && Server.LogSwitch)
                {
                    if (!Server.LogQueue.IsEmpty)
                    {
                        if (Server.LogQueue.TryDequeue(out string log))
                        {
                            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                              {
                                  Logs.Add(new ErrorLog(log));
                              }));
                        }
                    }
                }
            }
        }

        #region Properties

        private Connect _Server;
        /// <summary>
        /// 服务端
        /// </summary>
        public Connect Server
        {
            get => _Server;
            set => SetProperty(ref _Server, value);
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
                if (Server != null) Server.IP_SerialPort = value;
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
                if (Server != null) Server.Port = value;
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
                    while (Server.DataItems.Any(i => i.Address == nextAddr))
                        nextAddr++;

                    Server.DataItems.Add(new DataItem
                    {
                        Address = nextAddr,
                        Value = 0,
                        Description = "",
                    });
                    break;

                case "Remove":
                    if (SelectedDataItem != null)
                    {
                        SelectedDataItem.PropertyChanged -= OnDataItemPropertyChanged;
                        Server.DataItems.Remove(SelectedDataItem);
                    }
                    break;

                case "Save":
                    _ConnectsModel.SaveConnects();
                    Server.Log("地址表已保存");
                    break;
            }
        }

        private void OnDataItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Server is not ModbusTcpServer modbusServer) return;
            if (sender is not DataItem item) return;
            _ConnectsModel.ApplyDataItemToServer(modbusServer, item);

        }

        private void HookPropertyChangedOnAllItems()
        {
            if (Server?.DataItems == null) return;
            foreach (var item in Server.DataItems)
            {
                item.PropertyChanged -= OnDataItemPropertyChanged; // 去重
                item.PropertyChanged += OnDataItemPropertyChanged;
            }
        }

        #endregion
    }
}
