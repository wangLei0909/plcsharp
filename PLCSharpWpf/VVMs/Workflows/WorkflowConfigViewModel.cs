using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.Models;
using PLCSharp.VVMs.Homepage;
using PLCSharp.VVMs.Workflows.Script;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.Workflows
{
    /// <summary>
    /// 工作流配置视图模型
    /// </summary>
    public class WorkflowConfigViewModel : DialogAwareBase
    {
        /// <summary>
        /// 工作流配置视图模型
        /// </summary>
        public WorkflowConfigViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)

        {
            GlobalModel = container.Resolve<GlobalModel>();
        }

        private AsyncDelegateCommand _Save;
        /// <summary>
        /// 保存
        /// </summary>
        public AsyncDelegateCommand Save =>
            _Save ??= new AsyncDelegateCommand(ExecuteSaveAsync);

        private async Task ExecuteSaveAsync()
        {
            await CustomTextEditor.FormatCodeAsync();
            GlobalModel.WorkflowsModel.Manage.Execute("Save");

        }

        private AsyncDelegateCommand _Compile;
        /// <summary>
        /// 编译
        /// </summary>
        public AsyncDelegateCommand Compile =>
            _Compile ??= new AsyncDelegateCommand(ExecuteCompileAsync);

        private async Task ExecuteCompileAsync()
        {

            await CustomTextEditor.FormatCodeAsync();
            GlobalModel.WorkflowsModel.Manage.Execute("Save");
            SelectedFlowTask.Compile.Execute();
        }

        private Workflow _SelectedFlowTask;
        /// <summary>
        /// SelectedFlowTask
        /// </summary>
        public Workflow SelectedFlowTask
        {
            get { return _SelectedFlowTask; }
            set { SetProperty(ref _SelectedFlowTask, value); }
        }

        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            SelectedFlowTask = parameters.GetValue<Workflow>("SelectedTask");
        }

        private DelegateCommand _Run;
        /// <summary>
        /// 运行
        /// </summary>
        public DelegateCommand Run =>
            _Run ??= new DelegateCommand(ExecuteRun);

        void ExecuteRun()
        {
            if (SelectedFlowTask.AutomaticExecution == false)
            {
                SelectedFlowTask.Run(GlobalModel);
            }
            else
            {
                System.Windows.MessageBox.Show("自动运行中，不能手动运行!"); ;
            }
        }
        readonly GlobalModel GlobalModel;


        private DelegateCommand<string> _FastEntry;
        /// <summary>
        /// FastEntry
        /// </summary>
        public DelegateCommand<string> FastEntry =>
            _FastEntry ??= new DelegateCommand<string>(ExecuteFastEntry);

        void ExecuteFastEntry(string cmd)
        {
            switch (cmd)
            {
                case "网络收发":
                    CustomTextEditor.Insert(@"
                        case 0:
                       
                                CurrConnect = globalModel.GetConnect(""网络连接名"");
                                _ = CurrConnect.SendAsync(""要发送的消息""); 
                                flow.Step++;
                       
                        break;
                        case 1:
                
                   
                                if (string.IsNullOrEmpty(CurrConnect.ReceiveInfo))
                                {
 
                                        flow.Step++;

                            }
                        break;
                    ");
                    break;

                case "视觉功能":
                    CustomTextEditor.Insert(@"
                        case 0:
                          
                                CurrVF= globalModel.GetVisionFunction(""视觉功能名"");
                                CurrVF.Flow.Reset();
                                flow.Step++;
                      
                           break;
                        case 1:
                            {
                            if(CurrVF.RunAll(CurrVF.Flow))
                                {
                                    flow.Step++;

                                }else if (flow.CheckStepTime(3))
                                     throw new Exception(""视觉功能超时!"");
                                }     
                            }                       

                            break;
                    ");
                    break;
                case "显示状态":
                    CustomTextEditor.Insert(@"
                        case 0:
                            {
                                var stateControl = globalModel.GetCustomControl(""控件名称"");
                                stateControl.Params.CellInfos[0].State = CellState.OK;
                                flow.Step++;
                            }
                        break;
                    ");
                    break;

                case "运动控制":
                    CustomTextEditor.Insert(@"
                        case 0:
                            {
                                var point = globalModel.ControllersModel.AxisPoints.FirstOrDefault(p => p.Name == ""点位名称"");
                                if (point != null)
                                {
                                    point.Run();
                                    flow.Step++;
                                }
                            }
                        break;
                        case 1:
                            {
                                var point = globalModel.ControllersModel.AxisPoints.FirstOrDefault(p => p.Name == ""点位名称"");
                                if (point != null && point.IsDone())
                                {
                                    flow.Step++;
                                }
                                else if (flow.CheckStepTime(10))
                                    throw new Exception(""点位运动超时!"");
                            }
                        break;
                    ");
                    break;
                case "变量赋值":
                    CustomTextEditor.Insert(@"
                        case 0:
                            {
                                var variable = globalModel.GetVariable(""变量名"");
                                variable.Value = 100; // 设置变量值
                                flow.Step++;
                            }
                        break;
                    ");
                    break;

                case "延时等待":
                    CustomTextEditor.Insert(@"
                        case 0:
                            {
                                // 开始等待
                                flow.Step++;
                            }
                        break;
                        case 1:
                            if (flow.CheckStepTime(2)) // 等待2秒
                            {
                                flow.Step++;
                            }
                        break;
                    ");
                    break;

                case "机器人运动":
                    CustomTextEditor.Insert(@"
                        case 0:
                            {
                                var robot = globalModel.GetRobot(""机器人名称"");
                                if (robot != null)
                                {
                                    var point = robot.Points.FirstOrDefault(p => p.Name == ""点位名称"");
                                    if (point != null)
                                    {
                                        robot.RunPoint(point);
                                        flow.Step++;
                                    }
                                }
                            }
                        break;
                        case 1:
                            if (robot.) // 等待运动完成
                            {
                                flow.Step++;
                            }
                        break;
                    ");
                    break;

            }

        }

        /// <summary>
        /// CustomTextEditor
        /// </summary>
        public CustomTextEditor CustomTextEditor { get; set; }
        int index = 0;
        CustomControl stateControl;
        /// <summary>
        /// Demo
        /// </summary>
        /// <param name="globalModel">全局模型</param>
        /// <param name="flow">流程状态模型</param>
        public void Demo(GlobalModel globalModel, FlowModel flow)
        {
            switch (flow.Step)
            {


            }


        }
    }

}
