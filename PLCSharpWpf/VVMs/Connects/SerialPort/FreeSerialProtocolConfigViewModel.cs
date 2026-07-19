using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace PLCSharp.VVMs.Connects.SerialPort
{
    /// <summary>
    /// Free串口协议配置视图模型
    /// </summary>
    class FreeSerialProtocolConfigViewModel : ValidateBase, IDialogAware
    {
        /// <summary>
        /// Free串口协议配置视图模型
        /// </summary>
        public FreeSerialProtocolConfigViewModel(IContainerExtension container)
        {
            _ConnectsModel = container.Resolve<ConnectsModel>();
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();
        }
        private readonly ConnectsModel _ConnectsModel;
        private FreeSerialProtocol _Client;
        /// <summary>
        /// 客户端
        /// </summary>
        public FreeSerialProtocol Client
        {
            get { return _Client; }
            set { SetProperty(ref _Client, value); }
        }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title => "自由串口协议";

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
            Client = _ConnectsModel.Connects.FirstOrDefault(c => c.Name == name) as FreeSerialProtocol;
            if (Client == null) return;

            Client.LogSwitch = true;

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
                        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(new Action(() =>
                        {
                            Logs.Add(new ErrorLog(log));
                        }));
                    }
                }
            }
        }


        private string[] _Ports;

        /// <summary>
        /// Ports
        /// </summary>
        public string[] Ports
        {
            get { return _Ports; }
            set { SetProperty(ref _Ports, value); }
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

        private string _SendString;
        /// <summary>
        /// 发送String
        /// </summary>
        public string SendString
        {
            get { return _SendString; }
            set { SetProperty(ref _SendString, value); }
        }
        private string _SendHex;
        /// <summary>
        /// 发送Hex
        /// </summary>
        public string SendHex
        {
            get { return _SendHex; }
            set { SetProperty(ref _SendHex, value); }
        }

        private DelegateCommand _HexToString;

        /// <summary>
        /// HexToString
        /// </summary>
        public DelegateCommand HexToString =>
             _HexToString ??= new DelegateCommand(ExecuteHexToString);

        private void ExecuteHexToString()
        {
            //"61 62 63 0d 0A"
            if (string.IsNullOrEmpty(_SendHex)) return;

            List<char> bytes = [];

            foreach (var item in _SendHex)
            {
                if (item == 32) continue;
                if (item > 47 && item < 58
                 || item > 64 && item < 71
                 || item > 96 && item < 103)
                { bytes.Add(item); continue; }
                Client.Log($"转换失败，非法字符：{item}");
                return;
            }
            //"6162630d0A"

            if (bytes.Count < 2 || bytes.Count % 2 != 0)
            {
                Client.Log("转换失败,检查输入");
                return;
            }

            StringBuilder stringBuilder = new();
            for (int i = 0; i < bytes.Count; i++)
            {
                stringBuilder.Append(bytes[i]);
                if (i % 2 == 1) stringBuilder.Append('-');
            }

            var byteStrings = stringBuilder.ToString().Split('-');

            stringBuilder.Clear();
            foreach (var item in byteStrings)
            {
                if (string.IsNullOrEmpty(item)) continue;
                stringBuilder.Append((char)Convert.ToInt32(item, 16));
            }

            SendString = stringBuilder.ToString();
        }

        private DelegateCommand _StringToHex;

        /// <summary>
        /// StringToHex
        /// </summary>
        public DelegateCommand StringToHex =>
             _StringToHex ??= new DelegateCommand(ExecuteStringToHex);

        private void ExecuteStringToHex()
        {
            if (string.IsNullOrEmpty(_SendString)) return;
            var bytes = Encoding.ASCII.GetBytes(_SendString); // abc -> {97, 98, 99} ( {0x61,0x62,0x63} )
            SendHex = BitConverter.ToString(bytes, 0).Replace("-", " "); //"61 62 63"
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

                case "Save":
                    _ConnectsModel.SaveConnects();
                    break;
            }
        }
        private DelegateCommand<object> _Send;
        /// <summary>
        /// 发送
        /// </summary>
        public DelegateCommand<object> Send =>
            _Send ??= new DelegateCommand<object>(ExecuteSend);

        void ExecuteSend(object param)
        {
            var cmd = (string)param;

            switch (cmd)
            {
                case "String":
                    _ = Client.SendMsgAsync(SendString);
                    break;
                case "Hex":
                    if (Connect.HexStringToBytes(SendHex, out List<byte> byes, out string format))
                    {
                        SendHex = format;

                        _ = Client.SendDataAsync([.. byes]);

                    }
                    break;
            }
        }

    }
}
