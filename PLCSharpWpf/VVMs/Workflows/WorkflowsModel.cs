using PLCSharp.Core.Prism;
using PLCSharp.Models;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.Windows;

namespace PLCSharp.VVMs.Workflows
{


    /// <summary>
    /// Workflows模型
    /// </summary>
    [Model]
    public class WorkflowsModel : ModelBase
    {

        readonly IContainerExtension Container;
        /// <summary>
        /// Workflows模型
        /// </summary>
        public WorkflowsModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            Container = container;
        }
        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="globalModel">全局模型</param>
        public void Init(GlobalModel globalModel)
        {
            GlobalModel = globalModel;
        }

        private ObservableCollection<Workflow> _Workflows = [];
        /// <summary>
        /// Workflows
        /// </summary>
        public ObservableCollection<Workflow> Workflows
        {
            get { return _Workflows; }
            set { SetProperty(ref _Workflows, value); }
        }


        private Workflow _SelectedTask;
        /// <summary>
        /// 配置项 
        /// </summary>
        public Workflow SelectedTask
        {
            get { return _SelectedTask; }
            set { SetProperty(ref _SelectedTask, value); }
        }

        private DelegateCommand<string> _Manage;
        /// <summary>
        /// 管理
        /// </summary>
        public DelegateCommand<string> Manage =>
            _Manage ??= new DelegateCommand<string>(ExecuteManage);

        void ExecuteManage(string cmd)
        {
            switch (cmd)
            {
                case "New":
                    var newTask = new Workflow();
                    newTask.Code =
@"
using System;
using System.Linq;
using PLCSharp.Models;
using PLCSharp.Core.Common;
using PLCSharp.VVMs.Connects;
using PLCSharp.VVMs.GlobalVariables;
using PLCSharp.VVMs.Homepage;
using PLCSharp.VVMs.MotionController;
using PLCSharp.VVMs.Robots;
using PLCSharp.VVMs.Vision;

public class Extension_" + newTask.ID.ToString().Replace("-", "") +
@" 
{
    private Robot CurrRobot;
    private CustomControl CurrtateControl;
    private VisionFunction CurrVF;
    private Connect CurrConnect;
    private AxisPoint CurrAxisPoint;
    private Variable CurrVariable;
    private CustomControl CurrControl;
    public void Run(GlobalModel globalModel,FlowModel flow)
    {
        switch (flow.Step)
        {
            //插入代码              
            case 0:
                break;
 
        }
    }
}
";

                    Workflows.Add(newTask);

                    newTask.Start(Container.Resolve<GlobalModel>());
                    break;
                case "Remove":
                    if (SelectedTask != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除任务 [{SelectedTask.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        var task = _DatasContext.Workflows.Where(c => c.ID == SelectedTask.ID).FirstOrDefault();
                        if (task != null)
                        {
                            task.Stop();
                            _DatasContext.Workflows.Remove(task);
                            _DatasContext.Save();
                        }
                        var name = SelectedTask.Name;
                        Workflows.Remove(SelectedTask);
                        SendInfoDialog($"已删除：{name}");

                    }
                    break;
                case "Save":
                    var names = new List<string>();

                    foreach (var item in Workflows)
                    {
                        if (string.IsNullOrEmpty(item.Name))
                        {
                            SendInfoDialog($"保存失败，名称{item.Name}不合适！");
                            return;
                        }

                        if (names.Contains(item.Name))
                        {
                            SendInfoDialog($"保存失败，重复的名称{item.Name}！");
                            return;
                        }
                        else
                        {
                            names.Add(item.Name);
                        }
                    }


                    foreach (var item in Workflows)
                    {
                        if (_DatasContext.Workflows.Any(h => h.ID == item.ID) == false)
                        {
                            item.RecipeID = _DatasContext.CurrentRecipe.ID;
                            _DatasContext.Workflows.Add(item);

                        }
                        else
                        {
                            var newitem = _DatasContext.Workflows.Where(c => c.ID == item.ID).FirstOrDefault();

                            newitem.RecipeID = item.RecipeID;
                            newitem.Name = item.Name;
                            newitem.Code = item.Code;
                            newitem.Comment = item.Comment;
                            newitem.AutomaticExecution = item.AutomaticExecution;
                        }


                    }
                    SelectedTask.Prompt = "";
                    _DatasContext.Save();
                    break;

                case "Config":
                    if (editting) return;
                    if (SelectedTask == null) return;
                    if (string.IsNullOrEmpty(SelectedTask.Name)) return;
                    var dialogParams = new DialogParameters
                        {
                            { "SelectedTask", SelectedTask}
                        };
                    editting = true;
                    _dialogService.Show("WorkflowConfig", dialogParams, r =>
                    {
                        editting = false;

                    });
                    break;
            }
        }
        private bool editting;
    }
}
