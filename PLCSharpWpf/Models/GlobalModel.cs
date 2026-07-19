using MiniExcelLibs;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.Core.Tools;
using PLCSharp.Core.UserControls;
using PLCSharp.VVMs.Connects;
using PLCSharp.VVMs.GlobalVariables;
using PLCSharp.VVMs.Homepage;
using PLCSharp.VVMs.ModeState;
using PLCSharp.VVMs.MotionController;
using PLCSharp.VVMs.Recipe;
using PLCSharp.VVMs.Robots;
using PLCSharp.VVMs.Vision;
using PLCSharp.VVMs.Workflows;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;


namespace PLCSharp.Models
{
    /// <summary>
    /// 全局模型
    /// </summary>
    [Model]
    public partial class GlobalModel : ModelBase
    {
        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            NatashaManagement
                 //获取链式构造器
                 .GetInitializer()
                 //使用引用程序集中的命名空间
                 .WithMemoryUsing()
                 //使用内存中的元数据
                 .WithMemoryReference()
                 //注册域构造器
                 .Preheating<NatashaDomainCreator>();

            foreach (var item in _DatasContext.Recipes)
            {
                Recipes.Add(item.DeepCopy());

            }

            if (Recipes.Count == 0)
            {
                var recipe = new Recipe
                {
                    Name = "First",
                    Comment = "",
                    Prompt = "默认配方",
                    Current = true
                };
                Recipes.Add(recipe);
                RecipeSelected = recipe;
                ExecuteRecipeManage("Save");
                CurrentRecipe = recipe;
            }
            else
            {
                CurrentRecipe = Recipes.Where(r => r.Current).FirstOrDefault();

                if (CurrentRecipe == null)
                {
                    CurrentRecipe = Recipes.FirstOrDefault();
                    CurrentRecipe.Current = true;
                    RecipeSelected = CurrentRecipe;
                    ExecuteRecipeManage("Save");
                }
            }

            VariablesModel = container.Resolve<VariablesModel>();
            VariablesModel.Init(this);

            Connects = container.Resolve<ConnectsModel>();

            ControllersModel = container.Resolve<ControllersModel>();
            ControllersModel.Init(this);

            WorkflowsModel = container.Resolve<WorkflowsModel>();

            VisionsModel = container.Resolve<VisionsModel>();

            RobotModel = container.Resolve<RobotModel>();
            VisionsModel.Init(this);
            RobotModel.Init(this);
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;
            bkgWorker.RunWorkerAsync();

            //注册发送给errLog的消息
            ea.GetEvent<MessageEvent>().Subscribe(
                ErrMessageReceived,
                ThreadOption.UIThread,
                false,
                (filter) => filter.Target.Contains("errLog"));

            //注册配方变更
            ea.GetEvent<MessageEvent>().Subscribe(
                RecipeChanged,
                ThreadOption.UIThread,
                false,
                (filter) => filter.Target.Contains("RecipeChanged"));
            //注册退出
            ea.GetEvent<MessageEvent>().Subscribe(
                Exit,
                ThreadOption.UIThread,
                false,
                (filter) => filter.Target.Contains("Exit"));

        }

        private void Exit(Message message)
        {

            if (ModeState.State.Execute)
            {

                SendInfoDialog("无法退出，请先停止运行！");
                return;
            }

            MessageBoxResult result = MessageBox.Show("确认退出吗？请确保安全！", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
            {
                // 用户点击了“确定”按钮
                MessageBoxResult resultSeconnd = MessageBox.Show("确认退出吗？即将退出控制！", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (resultSeconnd == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        #region 全局变量


        /// <summary>
        /// 获取变量
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>返回结果</returns>
        public Variable GetVariable(string name)
        {
            var variable = VariablesModel.Variables.FirstOrDefault(v => v.Name == name)
              ?? throw new Exception("全局变量未配置!");
            return variable;
        }

        #endregion

        #region 硬件
        /// <summary>
        /// 连接模型
        /// </summary>
        public ConnectsModel Connects { get; set; }
        /// <summary>
        /// Controllers模型
        /// </summary>
        public ControllersModel ControllersModel { get; set; }

        /// <summary>
        /// 机器人模型
        /// </summary>
        public RobotModel RobotModel { get; set; }

        /// <summary>
        /// 全局变量
        /// </summary>
        public VariablesModel VariablesModel { get; set; }


        /// <summary>
        /// 获取连接
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>返回结果</returns>
        public Connect GetConnect(string name)
        {
            var connect = Connects.Connects.FirstOrDefault(c => c.Name == name)
                                ?? throw new Exception("网络未配置!");

            return connect;
        }
        /// <summary>
        /// 获取Robot
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>返回结果</returns>
        public Robot GetRobot(string name)
        {
            var robot = RobotModel.Robots.FirstOrDefault(c => c.Name == name)
                                ?? throw new Exception("Robot未配置!");

            return robot;
        }
        /// <summary>
        /// 获取VisionFunction
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>返回结果</returns>
        public VisionFunction GetVisionFunction(string name)
        {
            return VisionsModel.VisionFunctions.FirstOrDefault(v => v.Name == name)
                 ?? throw new Exception("视觉功能未配置!");
        }
        /// <summary>
        /// 获取图像数据
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>返回结果</returns>
        public ImageData GetImageData(string name)
        {
            var imageData = VisionsModel.ImageDatas.FirstOrDefault(v => v.Name == name)
                 ?? throw new Exception("全局图像未配置!");
            if (imageData.Mat == null) throw new Exception("全局图像未赋值!");
            return imageData;
        }


        #endregion

        #region 任务
        /// <summary>
        /// Workflows模型
        /// </summary>
        public WorkflowsModel WorkflowsModel { get; set; }
        /// <summary>
        /// 视觉模型
        /// </summary>
        public VisionsModel VisionsModel { get; set; }

        /// <summary>
        /// 获取Task
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>返回结果</returns>
        public Workflow GetTask(string name)
        {
            return WorkflowsModel.Workflows.FirstOrDefault(t => t.Name == name);
        }
        #endregion

    }
}