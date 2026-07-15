using PLCSharp.Core.Common;
using PLCSharp.Models;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace PLCSharp.VVMs.Workflows
{
    /// <summary>
    /// 工作流
    /// </summary>
    public class Workflow : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
        /// <summary>
        /// 配方标识
        /// </summary>
        public Guid RecipeID { get; set; }

        private string _Name;
        /// <summary>
        /// 配置项
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name != value)
                {
                    Prompt = "已修改，请保存";
                }
                SetProperty(ref _Name, value);
            }
        }

        private string _Prompt;
        /// <summary>
        /// 提示
        /// </summary>
        [NotMapped]
        public string Prompt
        {
            get { return _Prompt; }
            set { SetProperty(ref _Prompt, value); }
        }
        private double _CycleDelayTime = 0.01;
        /// <summary>
        /// 循环结束延迟
        /// </summary>
        public double CycleDelayTime
        {
            get { return _CycleDelayTime; }
            set { SetProperty(ref _CycleDelayTime, value); }
        }
        private string _Code;



        private double _CycleTime;
        /// <summary>
        /// CycleTime
        /// </summary>
        [NotMapped]
        public double CycleTime
        {
            get { return _CycleTime; }
            set { SetProperty(ref _CycleTime, value); }
        }

        /// <summary>
        /// 配置项
        /// </summary>
        public string Code
        {
            get { return _Code; }
            set
            {
                if (_Code != value)
                {
                    IsCompiled = false;
                    Prompt = "已修改，请编译";
                }
                SetProperty(ref _Code, value);
            }
        }
        private string _Comment;
        /// <summary>
        /// 备注
        /// </summary>
        public string Comment

        {
            get { return _Comment; }
            set
            {
                if (_Comment != value)
                {
                    Prompt = "已修改，请保存";
                }
                SetProperty(ref _Comment, value);
            }
        }

        private bool _AutomaticExecution;
        /// <summary>
        /// 脚本自动运行
        /// </summary>
        public bool AutomaticExecution
        {
            get { return _AutomaticExecution; }
            set { SetProperty(ref _AutomaticExecution, value); }
        }

        private bool _IsCompiled;
        /// <summary>
        /// IsCompiled
        /// </summary>
        [NotMapped]
        public bool IsCompiled
        {
            get { return _IsCompiled; }
            set { SetProperty(ref _IsCompiled, value); }
        }

        private bool _IsRuning;
        /// <summary>
        /// IsRuning
        /// </summary>
        [NotMapped]
        public bool IsRuning
        {
            get { return _IsRuning; }
            set { SetProperty(ref _IsRuning, value); }
        }

        /// <summary>
        /// Assembly
        /// </summary>
        [NotMapped]
        public Assembly Assembly { get; set; }

        private string _DebugLog;
        /// <summary>
        /// Debug日志
        /// </summary>
        [NotMapped]
        public string DebugLog
        {
            get { return _DebugLog; }
            set { SetProperty(ref _DebugLog, value); }
        }


        private bool _Exception;
        /// <summary>
        /// 异常
        /// </summary>
        [NotMapped]
        public bool Exception
        {
            get { return _Exception; }
            set { SetProperty(ref _Exception, value); }
        }

        AssemblyCSharpBuilder AssemblyCSharpBuilder;


        private DelegateCommand _Compile;
        /// <summary>
        /// 编译
        /// </summary>
        public DelegateCommand Compile =>
            _Compile ??= new DelegateCommand(ExecuteCompile);

        void ExecuteCompile()
        {

            lock (this)
            {
                try
                {
                    Flow.Reset();
                    AssemblyCSharpBuilder = new();
                    DebugLog = "编译中，请稍等。\n";
                    AssemblyCSharpBuilder.ConfigLoadContext(ctx => ctx.AddReferenceAndUsingCode<object>());

                    AssemblyCSharpBuilder.Add(Code);
                    AssemblyCSharpBuilder.LogCompilationEvent += (log) => { DebugLog += log.ToString(); };

                    Assembly = AssemblyCSharpBuilder.GetAssembly();
                    DebugLog += "编译完成。\n";
                    Prompt = "";
                    IsCompiled = true;
                    Exception = false;
                    var typeName = "Extension_" + ID.ToString().Replace("-", "");
                    var extType = Assembly.GetTypes().FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
                    if (extType != null)
                    {
                        var instance = Activator.CreateInstance(extType)!;
                        action = Assembly.GetDelegateFromShortName<Action<GlobalModel, FlowModel>>(typeName, "Run", target: instance);
                    }
                    else
                    {
                        DebugLog += "编译错误：无法找到 Extension 类型。\n";
                    }
                }
                catch (Exception e)
                {
                    DebugLog += e.Message;

                }


            }
        }

        private Action<GlobalModel, FlowModel> action;
        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="globalModel">全局模型</param>
        public void Run(GlobalModel globalModel)
        {
            if (Assembly == null || !IsCompiled || Exception)
            {
                IsRuning = false;
                return;
            }

            try
            {
                IsRuning = true;
                action?.Invoke(globalModel, Flow);
            }
            catch (Exception ex)
            {
                IsRuning = false;
                DebugLog += $"运行时异常: {ex}\n";
                globalModel.SendErr($"运行时异常: {ex}\n");
                Exception = true;
            }

        }

        private BackgroundWorker _BackgroundWorker;
        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="globalModel">全局模型</param>
        public void Start(GlobalModel globalModel)
        {
            GlobalModel = globalModel;
            _BackgroundWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            _BackgroundWorker.DoWork += BackgroundWork;
            _BackgroundWorker.RunWorkerAsync();
        }
        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (_BackgroundWorker != null && _BackgroundWorker.IsBusy)
            {
                _BackgroundWorker.CancelAsync();
            }
            while (_BackgroundWorker.IsBusy)
            {

            }
            IsRuning = false;
        }
        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            var sw = Stopwatch.StartNew();
            while (!worker.CancellationPending)
            {

                sw.Restart();
                Thread.Sleep((int)(CycleDelayTime * 1000));
                if (AutomaticExecution)
                {
        
                    Run(GlobalModel);
                }
                else
                {
                    IsRuning = false;
                }
                sw.Stop();
                CycleTime = sw.Elapsed.TotalSeconds;
                if (Exception)
                {
                    GlobalModel.ModeState.Sc.Stop = true;
                    if (GlobalModel.ModeState.State.Stopped)
                    {
                        Exception = false;
                       
                    }

                }
            }

        }
        GlobalModel GlobalModel;
        private FlowModel _Flow = new();
        /// <summary>
        /// 流程状态模型
        /// </summary>
        [NotMapped]
        public FlowModel Flow
        {
            get { return _Flow; }
            set { SetProperty(ref _Flow, value); }
        }


    }



}
