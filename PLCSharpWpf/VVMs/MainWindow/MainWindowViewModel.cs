using PLCSharp.Core.Prism;
using PLCSharp.Models;
using PLCSharp.VVMs.Authority;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using System.Diagnostics;
using System.Threading;
using System.Windows;


namespace PLCSharp.VVMs.MainWindow
{
    /// <summary>
    /// Main窗口视图模型
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "PLCSHARP";

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private readonly IDialogService _dialogService;
        /// <summary>
        /// 登录
        /// </summary>
        public LoginModel Login { get; set; }
        /// <summary>
        /// Navigate
        /// </summary>
        public NavigateModel Navigate { get; set; }

        /// <summary>
        /// Main窗口视图模型
        /// </summary>
        public MainWindowViewModel(IDialogService dialogService, IEventAggregator ea, IContainerExtension container)
        {
            _dialogService = dialogService;
            Login = container.Resolve<LoginModel>();
            Navigate = container.Resolve<NavigateModel>();
            _EventAggregator = ea;


            Thread thread = new(HardwareInfo)
            {
                IsBackground = true
            }
            ;
            thread.Start();
        }

        private void HardwareInfo(object obj)
        {
            // 获取当前进程信息
            Process currentProcess = Process.GetCurrentProcess();
            var time = 1000;
            while (true)
            {
                try
                {
                    //进程在所有CPU核心上实际消耗的总时间
                    DateTime startTime = DateTime.Now;
                    TimeSpan startCpuTime = currentProcess.TotalProcessorTime;
                    // 等待一段时间后再次读取数据
                    Thread.Sleep(time);
                    // 刷新性能计数器数据
                    currentProcess.Refresh();
                    //CPU总核心数
                    int coreCount = Environment.ProcessorCount;

                    //进程在所有CPU核心上实际消耗的总时间
                    TimeSpan endCpuTime = currentProcess.TotalProcessorTime;
                    //CPU总消耗时间
                    TimeSpan cpuUsed = endCpuTime - startCpuTime;
                    //侦测结束时间
                    DateTime endTime = DateTime.Now;
                    //侦测消耗时间
                    double checkTotalMilliseconds = (endTime - startTime).TotalMilliseconds;

                    //CPU在所有核心上的平均使用率 = CPU总消耗时间 除以 核心数后，得到平均每个核心消耗的时间，然后再除以 侦测消耗时间 乘以 100
                    double cpuUsageMultiCore = (cpuUsed.TotalMilliseconds / coreCount / checkTotalMilliseconds) * 100;


                    // 输出CPU时间
                    CpuUsage = cpuUsageMultiCore;

                    // 输出工作集内存（物理内存）
                    MemoryUsage = currentProcess.PrivateMemorySize64 / 1048576d;

                }
                catch (Exception)
                {


                }
            }
            //获取CPU和内存信息


        }

        /// <summary>
        /// 获取进程cpu使用率
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static dynamic GetProcessCpuPerUsage(Process process)
        {
            dynamic result = new System.Dynamic.ExpandoObject();
            result.ProcessId = process.Id;
            result.ProcessName = process.ProcessName;
            result.MemoryUsage = process.PrivateMemorySize64 / 1048576d;
            try
            {

                return result;
            }
            catch
            {
                result.ProcessCpuUsageAvg = 0;



            }
            return result;
        }
        private double _CpuUsage;
        /// <summary>
        /// CpuUsage
        /// </summary>
        public double CpuUsage
        {
            get { return _CpuUsage; }
            set { SetProperty(ref _CpuUsage, value); }
        }
        private double _MemoryUsage;
        /// <summary>
        /// MemoryUsage
        /// </summary>
        public double MemoryUsage
        {
            get { return _MemoryUsage; }
            set { SetProperty(ref _MemoryUsage, value); }
        }

        private List<string> showList = [];
        private DelegateCommand<string> _ShowDialog;
        /// <summary>
        /// 显示对话框
        /// </summary>
        public DelegateCommand<string> ShowDialog =>
            _ShowDialog ??= new DelegateCommand<string>(ExecuteShowDialog);

        void ExecuteShowDialog(string param)
        {

            if (showList.Contains(param))
            {
                return;
            }
     
            switch (param)
            {
                case "Login":
                    {
                        _dialogService.ShowDialog("Login", new DialogParameters($"message={"message:登陆"}"), r =>
                        {
                            if (r.Result == ButtonResult.Yes)
                            {
                              
                            }
                            else if (r.Result == ButtonResult.Retry)
                            {
                                _dialogService.ShowDialog("UserManage", new DialogParameters($"message={"message:管理"}"), r => { });
                            }
                        });
                    }
                    break;
                case "SystemConfig":
                    {
                        showList.Add(param);
                        if (Login.CurrentUser == null || Login.CurrentUser.Authority < Authority.Authority.SeniorMaintainer)
                        {
                            showList.Remove("SystemConfig");
                            MessageBox.Show("系统设置需要 高级维护 权限", "无权限");
                            return;
                        }
                        _dialogService.Show("SystemConfig", new DialogParameters($"message={"message:系统设置"}"), r =>
                        {
                            showList.Remove("SystemConfig");
                        });
                    }
                    break;
                case "ErrorLogs":
                    {

                        showList.Add("ErrorLogs");
                        _dialogService.Show("ErrorLogs", new DialogParameters($"message={"message:错误日志"}"), (Action<IDialogResult>)(r =>
                        {
                            showList.Remove("ErrorLogs");
                        }));
                    }
                    break;


            }
        }

        /// <summary>
        /// _EventAggregator
        /// </summary>
        protected readonly IEventAggregator _EventAggregator;
        private DelegateCommand _Exit;
        public DelegateCommand Exit =>
            _Exit ??= new DelegateCommand(ExecuteExit);

        void ExecuteExit()
        {

            _EventAggregator.GetEvent<MessageEvent>().Publish(new()
            {
                Target = "Exit"
            });


        }
    }
}