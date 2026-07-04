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
using PLCSharp.VVMs.Vision.Camera;
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
    public class GlobalModel : ModelBase
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
            if (connect.Connected == false)
            {
                throw new Exception("网络未连接!");
            }
            return connect;
        }
        /// <summary>
        /// 获取相机
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>返回结果</returns>
        public CameraBase GetCamera(string name)
        {
            return VisionsModel.Cameras.FirstOrDefault(c => c.Name == name)
                 ?? throw new Exception("相机未配置!");
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
        #region 日志
        private ObservableCollection<ErrorLog> _ErrorLogs = [];

        /// <summary>
        /// 错误Logs
        /// </summary>
        public ObservableCollection<ErrorLog> ErrorLogs
        {
            get { return _ErrorLogs; }
            set { SetProperty(ref _ErrorLogs, value); }
        }
        private TitleState _CurrentState = new();
        /// <summary>
        /// TitleState
        /// </summary>
        public TitleState TitleState
        {
            get { return _CurrentState; }
            set { SetProperty(ref _CurrentState, value); }
        }

        /// <summary>
        /// 添加错误日志
        /// </summary>
        /// <param name="message">消息内容</param>
        public void AddErrorLog(Message message)
        {
            if (TitleState.Info == message.Content)
            {
                TitleState.Background = Brushes.Red;
                return; //避免重复记录相同的错误
            }
            var newLog = new ErrorLog(message.Content);
            TitleState.Info = message.Content;
            TitleState.Background = Brushes.Red;
            _DatasContext.ErrorLogs.Add(newLog);
            ShowLog();

        }
        private readonly object loglock = new();
        private void ShowLog()
        {
            lock (loglock)
            {

                var showLog = _DatasContext.ErrorLogs.Where(e => e.Time.AddDays(1) >= DateTime.Now).OrderByDescending(l => l.Time).Take(30).ToList();

                foreach (var item in showLog)
                {
                    if (ErrorLogs.Contains(item))
                    {
                        continue;
                    }
                    ErrorLogs.Add(item);
                }

                var confirmedLog = ErrorLogs.Where(e => e.IsConfirm).ToList();

                //清理已确认的日志  
                foreach (var item in confirmedLog)
                {
                    ErrorLogs.Remove(item);
                }
            }
        }

        /// <summary>
        /// 错误日志对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        public void ErrorLogDialog(Message message)
        {
            _dialogService.Show("AlertDialog", new DialogParameters($"message={message.Content}"), r =>
            {
                if (r.Result == ButtonResult.Yes)
                {
                    //确认
                    var err = ErrorLogs.FirstOrDefault(e => e.Message == message.Content);
                    if (err != null)
                    {
                        err.IsConfirm = true;
                    }
                }
                else if (r.Result == ButtonResult.No)
                {
                    //取消
                }
            });
        }
        private void ErrMessageReceived(Message message)
        {
            switch (message.Type)
            {
                case ShowType.Record:

                    AddErrorLog(message);
                    break;

                case ShowType.ShowDialog:
                    ErrorLogDialog(message);
                    break;
                case ShowType.ShowDialogAndRecord:
                    AddErrorLog(message);
                    ErrorLogDialog(message);

                    break;
                default:
                    break;
            }

        }
        #endregion
        #region 模式与状态
        private ModeState _ModeState = new();
        /// <summary>
        /// ModeState
        /// </summary>
        public ModeState ModeState
        {
            get { return _ModeState; }
            set { SetProperty(ref _ModeState, value); }
        }

        private readonly BackgroundWorker bkgWorker;

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                try
                {
                    if (ModeState.Sc.Reset)
                    {
                        TitleState.Background = Brushes.Gray;
                    }
                    ModeState.Run();


                }
                catch (Exception ex)
                {
                    SendErr(ex.Message);
                    goto sleep;
                }

            sleep:

                Thread.Sleep(1);
            }
        }
        #endregion
        #region 配方


        private Recipe _CurrentRecipe;
        /// <summary>
        /// 当前配方
        /// </summary>
        public Recipe CurrentRecipe
        {
            get { return _CurrentRecipe; }
            set
            {
                SetProperty(ref _CurrentRecipe, value);

                _DatasContext.CurrentRecipe = value;
            }
        }
        private Recipe _RecipeSelected;
        /// <summary>
        /// 选中的配方
        /// </summary>
        public Recipe RecipeSelected
        {
            get { return _RecipeSelected; }
            set { SetProperty(ref _RecipeSelected, value); }
        }

        private ObservableCollection<Recipe> _Recipes = [];
        /// <summary>
        /// 配方集合
        /// </summary>
        public ObservableCollection<Recipe> Recipes
        {
            get { return _Recipes; }
            set { SetProperty(ref _Recipes, value); }
        }

        #region 配方管理
        private DelegateCommand<string> _RecipeManage;
        /// <summary>
        /// Recipe管理
        /// </summary>
        public DelegateCommand<string> RecipeManage =>
            _RecipeManage ??= new DelegateCommand<string>(ExecuteRecipeManage);

        void ExecuteRecipeManage(string cmd)
        {
            if (ModeState.State.Execute)
            {

                SendInfoDialog($"无法在运行状态操作配方，请先停止");
                return;
            }
            switch (cmd)
            {
                case "New":
                    Recipes.Add(new Recipe());
                    break;

                case "Save":
                    var names = new List<string>();

                    foreach (var item in Recipes)
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


                    foreach (var item in Recipes)
                    {
                        if (!_DatasContext.Recipes.Any(h => h.ID == item.ID))
                        {
                            _DatasContext.Recipes.Add(item);

                        }
                        else
                        {
                            var camera = _DatasContext.Recipes.Where(c => c.ID == item.ID).FirstOrDefault();
                            camera.Comment = item.Comment;
                        }
                    }
                    RecipeSelected.Prompt = "";
                    _DatasContext.Save();

                    break;

                case "Remove":
                    if (RecipeSelected != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除配方 [{RecipeSelected.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        if (RecipeSelected.ID != CurrentRecipe.ID)
                        {
                            {
                                var recipe = _DatasContext.Recipes.Where
                                     (r => r.ID == RecipeSelected.ID).FirstOrDefault();

                                if (recipe != null)
                                {
                                    _DatasContext.Recipes.Remove(recipe);
                                }

                                var name = RecipeSelected.Name;
                                Recipes.Remove(RecipeSelected);
                                SendInfoDialog($"已删除：{name}");
                                RecipeSelected = null;
                                _DatasContext.Save();
                            }
                        }
                        else
                        {
                            SendInfoDialog($"无法删除正在使用的配方，请切换到其它配方后再删除本配方!");
                        }
                    }
                    break;

                case "Current":
                    if (RecipeSelected != null)
                    {
                        foreach (var item in _DatasContext.Recipes)
                        {
                            item.Current = false;
                        }
                        var current = _DatasContext.Recipes.Where(r => r.ID == RecipeSelected.ID).FirstOrDefault();
                        if (current != null)
                        {
                            current.Current = true;
                            if (current != CurrentRecipe)
                            {
                                CurrentRecipe = current;

                                SendInfoDialog($"切换到配方：{CurrentRecipe.Name}");
                                _EventAggregator.GetEvent<MessageEvent>().Publish(new()
                                {
                                    Target = "RecipeChanged"
                                });
                            }
                        }
                    }



                    break;

                case "Copy":
                    RecipeCopy();
                    break;

                case "Export":
                    if (RecipeSelected == null) return;
                    {
                        var dialog = new Microsoft.Win32.SaveFileDialog
                        {
                            Filter = "Excel文件(*.xlsx)|*.xlsx",
                            FileName = $"配方_{RecipeSelected.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                        };
                        if (dialog.ShowDialog() != true) return;

                        try
                        {
                            var recipeId = RecipeSelected.ID;
                            var sheets = new Dictionary<string, object>();

                            // 全局变量
                            sheets["全局变量"] = _DatasContext.Variables
                                .Where(v => v.RecipeID == recipeId)
                                .Select(v => new
                                {
                                    ID = v.ID.ToString(),
                                    RecipeID = v.RecipeID.ToString(),
                                    v.Name,
                                    Type = v.Type.ToString(),
                                    v.ValueStr,
                                    默认值 = v.DefaultValueStr,
                                    v.Comment,
                                    断电保持 = v.RetainPersistent
                                }).ToList();

                            // 工艺流程
                            sheets["工艺流程"] = _DatasContext.Workflows
                                .Where(w => w.RecipeID == recipeId)
                                .Select(w => new
                                {
                                    ID = w.ID.ToString(),
                                    RecipeID = w.RecipeID.ToString(),
                                    w.Name,
                                    w.Comment,
                                    w.Code,
                                    自动运行 = w.AutomaticExecution,
                                    周期延时 = w.CycleDelayTime,
                                }).ToList();

                            // 视觉流程
                            sheets["视觉流程"] = _DatasContext.VisionFunctions
                                .Where(v => v.RecipeID == recipeId)
                                .Select(v => new
                                {
                                    ID = v.ID.ToString(),
                                    RecipeID = v.RecipeID.ToString(),
                                    v.Name,
                                    v.Comment,
                                    v.SerializedVisionParams,
                                    v.SerializedVisionFlows
                                }).ToList();

                            // 全局图像
                            sheets["全局图像"] = _DatasContext.ImageDatas
                                .Where(i => i.RecipeID == recipeId)
                                .Select(i => new
                                {
                                    ID = i.ID.ToString(),
                                    RecipeID = i.RecipeID.ToString(),
                                    i.Name,
                                    i.Comment,
                                    图像数据 = i.SerializedMat != null ? Convert.ToBase64String(i.SerializedMat) : ""
                                }).ToList();

                            // 运动控制点位
                            sheets["运动控制点位"] = _DatasContext.AxisPoints
                                .Where(a => a.RecipeID == recipeId)
                                .Select(a => new
                                {
                                    ID = a.ID.ToString(),
                                    RecipeID = a.RecipeID.ToString(),
                                    a.Name,
                                    a.X,
                                    a.Y,
                                    a.Z,
                                    a.U,
                                    安全高度 = a.ZSafe,
                                    关联轴X = a.AxisXName,
                                    关联轴Y = a.AxisYName,
                                    关联轴Z = a.AxisZName,
                                    关联轴U = a.AxisUName,
                                    X速度 = a.XRate,
                                    Y速度 = a.YRate,
                                    Z速度 = a.ZRate,
                                    U速度 = a.URate,
                                    a.SerializedParams
                                }).ToList();

                            // 机器人点位
                            sheets["机器人点位"] = _DatasContext.RobotPoints
                                .Where(r => r.RecipeID == recipeId)
                                .Select(r => new
                                {
                                    ID = r.ID.ToString(),
                                    RecipeID = r.RecipeID.ToString(),
                                    RobotID = r.RobotID.ToString(),
                                    r.Name,
                                    类型 = r.PointType,
                                    r.X,
                                    r.Y,
                                    r.Z,
                                    r.U,
                                    r.V,
                                    r.W,
                                    r.Rate,
                                    手系 = r.Hand.ToString(),
                                    工具号 = r.ToolNum,
                                    用户坐标 = r.UF,
                                    r.SerializedParams
                                }).ToList();

                            // 系统变量
                            sheets["系统变量"] = _DatasContext.SystemVariables
                                .Where(s => s.RecipeID == recipeId)
                                .Select(s => new
                                {
                                    ID = s.ID.ToString(),
                                    RecipeID = s.RecipeID.ToString(),
                                    s.Name,
                                    s.Comment,
                                    s.ValueStr,
                                    默认值 = s.DefaultValueStr,
                                    Type = s.Type.ToString(),
                                    断电保持 = s.RetainPersistent
                                }).ToList();

                            // 插补组
                            sheets["插补组"] = _DatasContext.InterpolationGroups
                                .Where(g => g.RecipeID == recipeId)
                                .Select(g => new
                                {
                                    ID = g.ID.ToString(),
                                    RecipeID = g.RecipeID.ToString(),
                                    g.Name,
                                    关联轴X = g.AxisXName,
                                    关联轴Y = g.AxisYName,
                                    g.SerializedParams,
                                    g.SerializedInterpolations
                                }).ToList();

                            // 画布配置
                            sheets["画布配置"] = _DatasContext.CanvasConfigs
                                .Where(c => c.RecipeID == recipeId)
                                .Select(c => new
                                {
                                    ID = c.ID.ToString(),
                                    RecipeID = c.RecipeID.ToString(),
                                    c.Rows,
                                    c.Columns
                                }).ToList();

                            // 自定义控件
                            sheets["自定义控件"] = _DatasContext.CustomControls
                                .Where(c => c.RecipeID == recipeId)
                                .Select(c => new
                                {
                                    ID = c.ID.ToString(),
                                    RecipeID = c.RecipeID.ToString(),
                                    c.Name,
                                    Type = c.Type.ToString(),
                                    c.Row,
                                    c.Column,
                                    c.RowSpan,
                                    c.ColumnSpan,
                                    c.Comment,
                                    c.SerializedParams,
                                    c.SerializedRangeValues
                                }).ToList();

                            MiniExcel.SaveAs(dialog.FileName, sheets);
                            SendInfoDialog($"配方 [{RecipeSelected.Name}] 已导出");
                        }
                        catch (Exception ex)
                        {
                            SendInfoDialog($"导出失败: {ex.Message}");
                        }
                    }
                    break;

                case "Import":
                    if (RecipeSelected == null) return;
                    {
                        var dialog = new Microsoft.Win32.OpenFileDialog
                        {
                            Filter = "Excel文件(*.xlsx)|*.xlsx"
                        };
                        if (dialog.ShowDialog() != true) return;

                        try
                        {
                            var recipeId = RecipeSelected.ID;

                            // 全局变量
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "全局变量"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.Variables.Any(v => v.ID == id)) continue;

                                var name = (string)row.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (_DatasContext.Variables.Any(v => v.Name == name && v.RecipeID == recipeId)) continue;

                                var v = new Variable
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Name = name,
                                    ValueStr = (string)row.ValueStr,
                                    DefaultValueStr = (string)row.默认值,
                                    Comment = (string)row.Comment,
                                    RetainPersistent = (bool)row.断电保持
                                };
                                if (Enum.TryParse<VariableDataType>((string)row.Type, out var vt))
                                    v.Type = vt;
                                _DatasContext.Variables.Add(v);
                            }

                            // 工艺流程
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "工艺流程"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.Workflows.Any(w => w.ID == id)) continue;

                                var name = (string)row.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (_DatasContext.Workflows.Any(w => w.Name == name && w.RecipeID == recipeId)) continue;

                                _DatasContext.Workflows.Add(new Workflow
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Name = name,
                                    Comment = (string)row.Comment,
                                    Code = (string)row.Code,
                                    AutomaticExecution = (bool)row.自动运行,
                                    CycleDelayTime = (double)row.周期延时
                                });
                            }

                            // 视觉流程
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "视觉流程"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.VisionFunctions.Any(v => v.ID == id)) continue;

                                var name = (string)row.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (_DatasContext.VisionFunctions.Any(v => v.Name == name && v.RecipeID == recipeId)) continue;

                                _DatasContext.VisionFunctions.Add(new VisionFunction
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Name = name,
                                    Comment = (string)row.Comment,
                                    SerializedVisionParams = (string)row.SerializedVisionParams,
                                    SerializedVisionFlows = (string)row.SerializedVisionFlows
                                });
                            }

                            // 全局图像
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "全局图像"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.ImageDatas.Any(i => i.ID == id)) continue;

                                var name = (string)row.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (_DatasContext.ImageDatas.Any(i => i.Name == name && i.RecipeID == recipeId)) continue;

                                var imgData = new ImageData
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Name = name,
                                    Comment = (string)row.Comment
                                };
                                var base64 = (string)row.图像数据;
                                if (!string.IsNullOrWhiteSpace(base64))
                                    imgData.SerializedMat = Convert.FromBase64String(base64);
                                _DatasContext.ImageDatas.Add(imgData);
                            }

                            // 运动控制点位
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "运动控制点位"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.AxisPoints.Any(a => a.ID == id)) continue;

                                var name = (string)row.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (_DatasContext.AxisPoints.Any(a => a.Name == name && a.RecipeID == recipeId)) continue;

                                _DatasContext.AxisPoints.Add(new AxisPoint
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Name = name,
                                    X = (double)row.X,
                                    Y = (double)row.Y,
                                    Z = (double)row.Z,
                                    U = (double)row.U,
                                    ZSafe = (double)row.安全高度,
                                    AxisXName = (string)row.关联轴X,
                                    AxisYName = (string)row.关联轴Y,
                                    AxisZName = (string)row.关联轴Z,
                                    AxisUName = (string)row.关联轴U,
                                    XRate = (double)row.X速度,
                                    YRate = (double)row.Y速度,
                                    ZRate = (double)row.Z速度,
                                    URate = (double)row.U速度,
                                    SerializedParams = (string)row.SerializedParams
                                });
                            }

                            // 机器人点位
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "机器人点位"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.RobotPoints.Any(r => r.ID == id)) continue;

                                var name = (string)row.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (_DatasContext.RobotPoints.Any(r => r.Name == name && r.RecipeID == recipeId)) continue;

                                var rp = new RobotPoint
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Name = name,
                                    PointType = (int)row.类型,
                                    X = (double)row.X,
                                    Y = (double)row.Y,
                                    Z = (double)row.Z,
                                    U = (double)row.U,
                                    V = (double)row.V,
                                    W = (double)row.W,
                                    Rate = (double)row.Rate
                                };
                                if (Guid.TryParse((string)row.RobotID, out var robotId))
                                    rp.RobotID = robotId;
                                if (Enum.TryParse<HandType>((string)row.手系, out var h))
                                    rp.Hand = h;
                                rp.ToolNum = (int)row.工具号;
                                rp.UF = (short)row.用户坐标;
                                rp.SerializedParams = (string)row.SerializedParams;
                                _DatasContext.RobotPoints.Add(rp);
                            }

                            // 系统变量
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "系统变量"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.SystemVariables.Any(s => s.ID == id)) continue;

                                var name = (string)row.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (_DatasContext.SystemVariables.Any(s => s.Name == name && s.RecipeID == recipeId)) continue;

                                var sv = new SystemVariable
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Name = name,
                                    Comment = (string)row.Comment,
                                    ValueStr = (string)row.ValueStr,
                                    DefaultValueStr = (string)row.默认值,
                                    RetainPersistent = (bool)row.断电保持
                                };
                                if (Enum.TryParse<VariableDataType>((string)row.Type, out var svt))
                                    sv.Type = svt;
                                _DatasContext.SystemVariables.Add(sv);
                            }

                            // 插补组
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "插补组"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.InterpolationGroups.Any(g => g.ID == id)) continue;

                                var name = (string)row.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (_DatasContext.InterpolationGroups.Any(g => g.Name == name && g.RecipeID == recipeId)) continue;

                                _DatasContext.InterpolationGroups.Add(new InterpolationGroup
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Name = name,
                                    AxisXName = (string)row.关联轴X,
                                    AxisYName = (string)row.关联轴Y,
                                    SerializedParams = (string)row.SerializedParams,
                                    SerializedInterpolations = (string)row.SerializedInterpolations
                                });
                            }

                            // 画布配置
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "画布配置"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.CanvasConfigs.Any(c => c.ID == id)) continue;

                                _DatasContext.CanvasConfigs.Add(new CanvasConfig
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Rows = (int)row.Rows,
                                    Columns = (int)row.Columns
                                });
                            }

                            // 自定义控件
                            foreach (var row in MiniExcel.Query(dialog.FileName, sheetName: "自定义控件"))
                            {
                                var idStr = (string)row.ID;
                                if (string.IsNullOrWhiteSpace(idStr)) continue;
                                if (!Guid.TryParse(idStr, out var id)) continue;
                                if (_DatasContext.CustomControls.Any(c => c.ID == id)) continue;

                                var name = (string)row.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;
                                if (_DatasContext.CustomControls.Any(c => c.Name == name && c.RecipeID == recipeId)) continue;

                                var cc = new CustomControl
                                {
                                    ID = id,
                                    RecipeID = recipeId,
                                    Name = name,
                                    Row = (int)row.Row,
                                    Column = (int)row.Column,
                                    RowSpan = (int)row.RowSpan,
                                    ColumnSpan = (int)row.ColumnSpan,
                                    Comment = (string)row.Comment,
                                    SerializedParams = (string)row.SerializedParams,
                                    SerializedRangeValues = (string)row.SerializedRangeValues
                                };
                                if (Enum.TryParse<ControlType>((string)row.Type, out var ct))
                                    cc.Type = ct;
                                _DatasContext.CustomControls.Add(cc);
                            }

                            _DatasContext.Save();
                            SendInfoDialog($"配方 [{RecipeSelected.Name}] 数据已导入，请切换配方刷新");
                        }
                        catch (Exception ex)
                        {
                            SendInfoDialog($"导入失败: {ex.Message}");
                        }
                    }
                    break;

            }
        }
        #endregion

        private void RecipeChanged(Message message)
        {
            //结束当前配方的任务

            foreach (var item in WorkflowsModel.Workflows)
            {
                try
                {
                    item.Stop();
                }
                catch (Exception)
                {

                }
            }
            // 重新加载画布配置和自定义控件

            CurrentCanvasConfig = _DatasContext.CanvasConfigs.Where(c => c.RecipeID == CurrentRecipe.ID).FirstOrDefault();

            if (CurrentCanvasConfig == null)
            {
                CurrentCanvasConfig = new CanvasConfig() { RecipeID = CurrentRecipe.ID };
                _DatasContext.Add(CurrentCanvasConfig);
            }

            var currRecipeCustomControls = _DatasContext.CustomControls.Where(c => c.RecipeID == CurrentRecipe.ID);
            CustomControls.Clear();
            foreach (var item in currRecipeCustomControls)
            {
                CustomControls.Add(item.DeepCopy());

            }
            var config = _DatasContext.CanvasConfigs.Where(c => c.RecipeID == CurrentRecipe.ID).FirstOrDefault();
            if (config != null)
            {
                CurrentCanvasConfig.Rows = config.Rows;
                CurrentCanvasConfig.Columns = config.Columns;
            }

            _EventAggregator.GetEvent<MessageEvent>().Publish(new()
            {
                Target = "UIReLoad"
            });
            //加载当前配方的用户变量列表
            VariablesModel.Variables.Clear();
            var variables = _DatasContext.Variables.Where(c => c.RecipeID == CurrentRecipe.ID);
            foreach (var item in variables)
            {
                if (item.RetainPersistent)
                {

                    item.Value = Variable.DynamicValue(item.ValueStr, item.Type);

                }
                else
                {
                    item.Value = Variable.DynamicValue(item.DefaultValueStr, item.Type);
                    item.DefaultValue = item.Value;

                }
                VariablesModel.Variables.Add(item);
            }
            //加载当前配方的系统变量列表
            VariablesModel.SystemVariables.Clear();
            var systemVariables = _DatasContext.SystemVariables.Where(c => c.RecipeID == CurrentRecipe.ID);
            foreach (var item in systemVariables)
            {
                if (item.RetainPersistent)
                {

                    item.Value = Variable.DynamicValue(item.ValueStr, item.Type);

                }
                else
                {
                    item.Value = Variable.DynamicValue(item.DefaultValueStr, item.Type);
                    item.DefaultValue = item.Value;

                }
                VariablesModel.SystemVariables.Add(item);
            }
            //加载当前配方的图像数据列表
            VisionsModel.ImageDatas.Clear();
            var currRecipeImageDatas = _DatasContext.ImageDatas.Where(c => c.RecipeID == CurrentRecipe.ID);
            foreach (var item in currRecipeImageDatas)
            {
                var itemcopy = item.DeepCopy();
                itemcopy.Prompt = "";
                VisionsModel.ImageDatas.Add(itemcopy);
            }

            //加载当前配方的视觉功能列表
            VisionsModel.VisionFunctions.Clear();
            var currRecipeVisionFunctions = _DatasContext.VisionFunctions.Where(c => c.RecipeID == CurrentRecipe.ID);
            foreach (var item in currRecipeVisionFunctions)
            {
                var itemcopy = item.DeepCopy();
                itemcopy.Prompt = "";
                itemcopy.VisionsModel = VisionsModel;
                itemcopy.ImageDatas = VisionsModel.ImageDatas;
                itemcopy.GlobalModel = this;
                VisionsModel.VisionFunctions.Add(itemcopy);
            }
            //加载当前配方的点位列表
            ControllersModel.AxisPoints.Clear();
            var currRecipeAxisPoints = _DatasContext.AxisPoints.Where(c => c.RecipeID == CurrentRecipe.ID);
            foreach (var item in currRecipeAxisPoints)
            {
                var axisPoint = item.DeepCopy();
                ControllersModel.AxisPoints.Add(axisPoint);
                var axisX = ControllersModel.Axes.Where(a => a.Name == axisPoint.AxisXName).FirstOrDefault();
                if (axisX != null)
                    axisPoint.AxisX = axisX;
                var axisY = ControllersModel.Axes.Where(a => a.Name == axisPoint.AxisYName).FirstOrDefault();
                if (axisY != null)
                    axisPoint.AxisY = axisY;
                var axisZ = ControllersModel.Axes.Where(a => a.Name == axisPoint.AxisZName).FirstOrDefault();
                if (axisZ != null)
                    axisPoint.AxisZ = axisZ;
                var axisU = ControllersModel.Axes.Where(a => a.Name == axisPoint.AxisUName).FirstOrDefault();
                if (axisU != null)
                    axisPoint.AxisU = axisU;
            }
            //加载当前配方的插补列表

            ControllersModel.InterpolationGroups.Clear();
            var currRecipeInterpolationGroups = _DatasContext.InterpolationGroups.Where(c => c.RecipeID == CurrentRecipe.ID);
            foreach (var item in currRecipeInterpolationGroups)
            {
                var itemcopy = item.DeepCopy();
                ControllersModel.InterpolationGroups.Add(itemcopy);
                var axisX = ControllersModel.Axes.Where(a => a.Name == itemcopy.AxisXName).FirstOrDefault();
                if (axisX != null)
                    itemcopy.AxisX = axisX;
                var axisY = ControllersModel.Axes.Where(a => a.Name == itemcopy.AxisYName).FirstOrDefault();
                if (axisY != null)
                    itemcopy.AxisY = axisY;
            }
            //加载当前配方的任务列表
            WorkflowsModel.Workflows.Clear();
            var currRecipeWorkflows = _DatasContext.Workflows.Where(c => c.RecipeID == CurrentRecipe.ID);
            foreach (var item in currRecipeWorkflows)
            {
                var itemcopy = item.DeepCopy();
                itemcopy.Prompt = "";
                WorkflowsModel.Workflows.Add(itemcopy);
                itemcopy.Compile.Execute();
                itemcopy.Start(this);
            }

        }

        private void RecipeCopy()
        {
            if (RecipeSelected == null) return;
            var newRecipe = new Recipe { Name = RecipeSelected.Name + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") };
            Recipes.Add(newRecipe);
            // 画布配置和自定义控件

            var canvasConfig = _DatasContext.CanvasConfigs.Where(c => c.RecipeID == RecipeSelected.ID).FirstOrDefault();

            if (canvasConfig != null)
            {
                var anvasConfigCopy = canvasConfig.DeepCopy();
                anvasConfigCopy.RecipeID = newRecipe.ID;
                anvasConfigCopy.ID = Guid.NewGuid();
                _DatasContext.CanvasConfigs.Add(anvasConfigCopy);
            }

            var customControls = _DatasContext.CustomControls.Where(c => c.RecipeID == RecipeSelected.ID);

            foreach (var item in customControls)
            {
                if (item != null)
                {
                    var itemCopy = item.DeepCopy();
                    itemCopy.RecipeID = newRecipe.ID;
                    itemCopy.ID = Guid.NewGuid();
                    _DatasContext.CustomControls.Add(itemCopy);
                }
            }

            // 变量列表

            var variables = _DatasContext.Variables.Where(c => c.RecipeID == RecipeSelected.ID);

            foreach (var item in variables)
            {
                if (item != null)
                {
                    var itemCopy = item.DeepCopy();
                    itemCopy.RecipeID = newRecipe.ID;
                    itemCopy.ID = Guid.NewGuid();
                    _DatasContext.Variables.Add(itemCopy);
                }
            }
            // 图像数据列表

            var imageDatas = _DatasContext.ImageDatas.Where(c => c.RecipeID == RecipeSelected.ID);
            foreach (var item in imageDatas)
            {
                if (item != null)
                {
                    var itemCopy = item.DeepCopy();
                    itemCopy.RecipeID = newRecipe.ID;
                    itemCopy.ID = Guid.NewGuid();
                    _DatasContext.ImageDatas.Add(itemCopy);
                }
            }
            // 视觉功能列表

            var visionFunctions = _DatasContext.VisionFunctions.Where(c => c.RecipeID == RecipeSelected.ID);
            foreach (var item in visionFunctions)
            {
                if (item != null)
                {
                    var itemCopy = item.DeepCopy();
                    itemCopy.RecipeID = newRecipe.ID;
                    itemCopy.ID = Guid.NewGuid();
                    _DatasContext.VisionFunctions.Add(itemCopy);
                }
            }
            // 点位列表

            var axisPoints = _DatasContext.AxisPoints.Where(c => c.RecipeID == RecipeSelected.ID);
            foreach (var item in axisPoints)
            {
                if (item != null)
                {
                    var itemCopy = item.DeepCopy();
                    itemCopy.RecipeID = newRecipe.ID;
                    itemCopy.ID = Guid.NewGuid();
                    _DatasContext.AxisPoints.Add(itemCopy);
                }
            }
            // 插补列表


            var interpolationGroups = _DatasContext.InterpolationGroups.Where(c => c.RecipeID == RecipeSelected.ID);
            foreach (var item in interpolationGroups)
            {
                if (item != null)
                {
                    var itemCopy = item.DeepCopy();
                    itemCopy.RecipeID = newRecipe.ID;
                    itemCopy.ID = Guid.NewGuid();
                    _DatasContext.InterpolationGroups.Add(itemCopy);
                }
            }
            // 任务列表

            var workflows = _DatasContext.Workflows.Where(c => c.RecipeID == RecipeSelected.ID);
            foreach (var item in workflows)
            {
                if (item != null)
                {
                    var itemCopy = item.DeepCopy();
                    itemCopy.RecipeID = newRecipe.ID;
                    itemCopy.ID = Guid.NewGuid();
                    _DatasContext.Workflows.Add(itemCopy);
                }
            }

        }
        #endregion
        #region 主页面
        private ObservableCollection<CustomControl> _CustomControls = [];
        /// <summary>
        /// 配置项
        /// </summary>
        public ObservableCollection<CustomControl> CustomControls
        {
            get { return _CustomControls; }
            set { SetProperty(ref _CustomControls, value); }
        }

        /// <summary>
        /// 获取Custom控件
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>返回结果</returns>
        public CustomControl GetCustomControl(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            else
            {
                return CustomControls.Where(c => c.Name == name).FirstOrDefault();
            }
        }

        public ImageEdit GetImageControl(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            else
            {
                var control = CustomControls.Where(c => c.Name == name).FirstOrDefault();

                if (control != null)
                {

                    if (control.UIElement is ImageEdit imageEdit)
                    {

                        return imageEdit;
                    }
                    else
                    {

                        return null;
                    }

                }
                else
                {
                    return null;
                }
            }
        }
        private CustomControl _SelectedCustControl;
        /// <summary>
        /// 配置项
        /// </summary>
        public CustomControl SelectedCustomControl
        {
            get { return _SelectedCustControl; }
            set { SetProperty(ref _SelectedCustControl, value); }
        }

        private CanvasConfig _CurrentCanvasConfig;
        /// <summary>
        /// 配置项
        /// </summary>
        public CanvasConfig CurrentCanvasConfig
        {
            get { return _CurrentCanvasConfig; }
            set { SetProperty(ref _CurrentCanvasConfig, value); }
        }

        private DelegateCommand _Apply;
        /// <summary>
        /// Apply
        /// </summary>
        public DelegateCommand Apply =>
            _Apply ??= new DelegateCommand(ExecuteApply);

        void ExecuteApply()
        {

            var config = _DatasContext.CanvasConfigs.Where(c => c.RecipeID == CurrentRecipe.ID).FirstOrDefault();
            if (config != null)
            {
                config.Rows = CurrentCanvasConfig.Rows;
                config.Columns = CurrentCanvasConfig.Columns;
            }
            else
            {
                _DatasContext.CanvasConfigs.Add(CurrentCanvasConfig);

            }
            //发布重新加载主页面通知
            _EventAggregator.GetEvent<MessageEvent>().Publish(new()
            {
                Target = "UIReLoad"
            });
            _DatasContext.Save();
        }
        #endregion

    }
}