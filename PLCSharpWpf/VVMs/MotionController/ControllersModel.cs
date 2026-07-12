using MiniExcelLibs;
using PLCSharp.Core.Prism;
using PLCSharp.Core.Tools;
using PLCSharp.Models;
using PLCSharp.VVMs.MotionController.Config;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// Controllers模型
    /// </summary>
    [Model]
    public class ControllersModel : ModelBase
    {
        /// <summary>
        /// Controllers模型
        /// </summary>
        public ControllersModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            AxisPointsConfig = new AxisPointsConfig
            {
                DataContext = this
            };
            InterpolationConfig = new InterpolationConfig
            {
                DataContext = this
            };
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;
        }
        #region BackgroundWork
        public void Start()
        {

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();

        }

        public void Stop()
        {

            bkgWorker.CancelAsync();

            while (bkgWorker.IsBusy)
            {
                Thread.Sleep(10);
            }

        }
        private BackgroundWorker bkgWorker;

        /// <summary>
        /// OnExit
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        protected override void OnExit(object sender, EventArgs e)
        {
            foreach (var axis in Axes)
            {
                axis.Stop();
            }
            Thread.Sleep(1000);
        }
        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                Thread.Sleep(1);
                for (int i = 0; i < AxisPoints.Count; i++)
                {
                    AxisPoints[i].Run();
                }

                for (int i = 0; i < Matrices.Count; i++)
                {
                    for (int j = 0; j < Matrices[i].Points.Count; j++)
                    {
                        Matrices[i].Points[j].Run();
                    }
                }
                for (int i = 0; i < InterpolationGroups.Count; i++)
                {
                    InterpolationGroups[i].Run();
                }


            }


        }
        #endregion

        #region Controller
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="globalModel">全局模型</param>
        public void Init(GlobalModel globalModel)
        {
            GlobalModel = globalModel;
            foreach (var item in _DatasContext.Controllers)
            {
                var controller = item.DeepCopy();
                controller.GlobalModel = GlobalModel;
                Controllers.Add(controller);
                var axes = _DatasContext.Axes.Where(a => a.ControllerID == controller.ID);
                foreach (var axis in axes)
                {
                    var axisCopy = axis.DeepCopy();
                    controller.Axes.Add(axisCopy);
                }
                controller.Init();
            }
            foreach (var item in _DatasContext.DI)
            {
                var di = item.DeepCopy();
                if (Controllers.Any(c => c.ID == di.ControllerID))
                {
                    Controllers.Where(c => c.ID == di.ControllerID).FirstOrDefault().DI.Add(di);
                }
            }
            foreach (var item in _DatasContext.DQ)
            {
                var dq = item.DeepCopy();
                if (Controllers.Any(c => c.ID == dq.ControllerID))
                {
                    Controllers.Where(c => c.ID == dq.ControllerID).FirstOrDefault().DQ.Add(dq);
                }
            }

            foreach (var controller in Controllers)
            {
                var sortDI = controller.DI.OrderBy(d => d.Number).ToList();

                controller.DI.Clear();

                foreach (var item in sortDI)
                {
                    controller.DI.Add(item);
                }

                var sortDQ = controller.DQ.OrderBy(d => d.Number).ToList();

                controller.DQ.Clear();

                foreach (var item in sortDQ)
                {
                    controller.DQ.Add(item);
                }
            }

            foreach (var controller in Controllers)
            {

                foreach (var axis in controller.Axes)
                {
                    Axes.Add(axis);
                }

                foreach (var item in controller.DI)
                {
                    DI.Add(item);
                }

                foreach (var item in controller.DQ)
                {
                    DQ.Add(item);
                }
            }

        }

        private double _Rate = 100;
        /// <summary>
        /// 
        /// </summary>
        public double Rate
        {
            get { return _Rate; }
            set { SetProperty(ref _Rate, value); }
        }

        /// <summary>
        /// DI
        /// </summary>
        public ObservableCollection<DI> DI { get; set; } = [];
        /// <summary>
        /// DQ
        /// </summary>
        public ObservableCollection<DQ> DQ { get; set; } = [];
        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }

        private ObservableCollection<Controller> _Controllers = [];

        /// <summary>
        /// Controllers
        /// </summary>
        public ObservableCollection<Controller> Controllers
        {
            get { return _Controllers; }
            set { SetProperty(ref _Controllers, value); }
        }

        private Controller _SelectedController;

        /// <summary>
        /// SelectedController
        /// </summary>
        public Controller SelectedController
        {
            get { return _SelectedController; }
            set
            {
                SetProperty(ref _SelectedController, value);
                if (_SelectedController == null) return;
                ControllerConfig = null;
                switch (_SelectedController.Type)
                {

                    case ControllerType.SMC304:
                        ControllerConfig = new SmcIO();
                        break;
                    case ControllerType.EMC_E3064_A08:
                        ControllerConfig = new EmcIO();
                        break;
                }
                if (ControllerConfig != null)
                    ControllerConfig.DataContext = this;
            }
        }

        private DelegateCommand<object> _IOManage;
        /// <summary>
        /// IO管理
        /// </summary>
        public DelegateCommand<object> IOManage =>
            _IOManage ??= new DelegateCommand<object>(ExecuteIOManage);

        void ExecuteIOManage(object param)
        {
            var cmd = param as string;
            switch (cmd)
            {
                case "Import":
                    {
                        Microsoft.Win32.OpenFileDialog ofd = new()
                        {
                            DefaultExt = ".*",
                            Filter = "Excel文件(*.xlsx)|*.xlsx"
                        };
                        if (ofd.ShowDialog() == true)
                        {
                            try
                            {
                                {

                                    var rows = MiniExcel.Query<DI>(ofd.FileName, sheetName: "DI");
                                    while (SelectedController.DI.Count < rows.Count())
                                    {
                                        var newDI = new DI()
                                        {
                                            ControllerID = SelectedController.ID,
                                            ControllerNo = SelectedController.ControllerNo,
                                            Number = (ushort)SelectedController.DI.Count
                                        };

                                        SelectedController.DI.Add(newDI);
                                        DI.Add(newDI);
                                    }


                                    foreach (var item in rows)
                                    {
                                        var i = SelectedController.DI.Where(d => d.Number == item.Number).FirstOrDefault();
                                        if (i != null)
                                        {

                                            i.LineNumber = item.LineNumber;
                                            i.Name = item.Name;

                                        }
                                    }
                                    var sort = SelectedController.DI.OrderBy(d => d.Number).ToList();
                                    foreach (var item in SelectedController.DI)
                                    {
                                        DI.Remove(item);
                                    }
                                    SelectedController.DI.Clear();

                                    foreach (var item in sort)
                                    {
                                        SelectedController.DI.Add(item);
                                        DI.Add(item);
                                    }
                                }



                                {

                                    var rows = MiniExcel.Query<DQ>(ofd.FileName, sheetName: "DQ");
                                    while (SelectedController.DQ.Count < rows.Count())
                                    {
                                        var newDQ = new DQ()
                                        {
                                            ControllerID = SelectedController.ID,
                                            ControllerNo = SelectedController.ControllerNo,
                                            Number = (ushort)SelectedController.DQ.Count
                                        };

                                        SelectedController.DQ.Add(newDQ);
                                        DQ.Add(newDQ);
                                    }


                                    foreach (var item in rows)
                                    {
                                        var i = SelectedController.DQ.Where(d => d.Number == item.Number).FirstOrDefault();
                                        if (i != null)
                                        {

                                            i.LineNumber = item.LineNumber;
                                            i.Name = item.Name;

                                        }
                                    }
                                    var sort = SelectedController.DQ.OrderBy(d => d.Number).ToList();

                                    foreach (var item in SelectedController.DQ)
                                    {
                                        DQ.Remove(item);
                                    }
                                    SelectedController.DQ.Clear();

                                    foreach (var item in sort)
                                    {
                                        SelectedController.DQ.Add(item);
                                        DQ.Add(item);
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                SendInfoDialog($"导入失败- {ex.Message}");

                            }
                        }
                    }
                    break;
                case "Export":
                    {
                        Microsoft.Win32.SaveFileDialog ofd = new()
                        {
                            DefaultExt = ".*",
                            Filter = "Excel文件(*.xlsx)|*.xlsx"
                        };

                        if (ofd.ShowDialog() == true)
                        {
                            try
                            {



                                var sheets = new Dictionary<string, object>
                                {
                                    ["DI"] = SelectedController.DI,
                                    ["DQ"] = SelectedController.DQ,
                                };
                                using var stream = File.Create(ofd.FileName);
                                MiniExcel.SaveAs(stream, sheets);


                            }
                            catch (Exception ex)
                            {
                                SendInfoDialog($"导出失败- {ex.Message}");

                            }
                        }


                    }
                    break;
                case "Save":
                    SaveDI();
                    SaveDQ();
                    break;
            }
        }

        /// <summary>
        /// 保存DI
        /// </summary>
        public void SaveDI()
        {
            var names = new List<string>();

            foreach (var item in SelectedController.DI)
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

            var thisControllerDI = _DatasContext.DI.Where(d => d.ControllerID == SelectedController.ID).ToList();
            if (thisControllerDI != null)
            {
                foreach (var item in thisControllerDI)
                {
                    _DatasContext.DI.Remove(item);
                }
            }
            foreach (var item in SelectedController.DI)
            {

                var newItem = item.DeepCopy();
                _DatasContext.DI.Add(newItem);

            }

        }
        /// <summary>
        /// 保存DQ
        /// </summary>
        public void SaveDQ()
        {
            var names = new List<string>();

            foreach (var item in SelectedController.DQ)
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

            var thisControllerDQ = _DatasContext.DQ
                                  .Where(d => d.ControllerID == SelectedController.ID).ToList();
            if (thisControllerDQ != null)
            {
                foreach (var item in thisControllerDQ)
                {
                    _DatasContext.DQ.Remove(item);
                }
            }
            foreach (var item in SelectedController.DQ)
            {
                var newItem = item.DeepCopy();
                _DatasContext.DQ.Add(newItem);
            }

        }
        private DelegateCommand<string> _ControllerManage;

        /// <summary>
        /// Controller管理
        /// </summary>
        public DelegateCommand<string> ControllerManage =>
            _ControllerManage ??= new DelegateCommand<string>(ExecuteControllerManage);

        void ExecuteControllerManage(string cmd)
        {
            switch (cmd)
            {
                case "New":
                    var controller = new Controller
                    {
                        GlobalModel = GlobalModel
                    };
                    Controllers.Add(controller);
                    controller.ControllerNo = (ushort)(Controllers.IndexOf(controller));
                    controller.Init();
                    break;

                case "Save":
                    foreach (var item in Controllers)
                    {
                        if (!_DatasContext.Controllers.Any(h => h.ID == item.ID))
                        {
                            _DatasContext.Controllers.Add(item);
                        }
                        else
                        {
                            var Controller = _DatasContext.Controllers.Where(c => c.ID == item.ID).FirstOrDefault();
                            Controller.Type = item.Type;
                            Controller.IP = item.IP;
                            Controller.Comment = item.Comment;
                            Controller.Params = item.Params;
                        }

                    }
                    _DatasContext.Save();
                    SelectedController.Prompt = "";
                    break;

                case "Remove":
                    if (SelectedController != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除控制器编号 [{SelectedController.ControllerNo}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        if (_DatasContext.Controllers.Any(h => h.ID == SelectedController.ID))
                        {
                            var remove = _DatasContext.Controllers.Where(h => h.ID == SelectedController.ID).FirstOrDefault();

                            _DatasContext.Controllers.Remove(remove);
                            SelectedController.Close();
                            var id = SelectedController.ID;
                            Controllers.Remove(SelectedController);
                            SendInfoDialog($"已删除控制器：{id}");
                            _DatasContext.Save();
                        }
                    }

                    break;
                case "Config":
                    ControllerConfig = null;
                    switch (_SelectedController.Type)
                    {

                        case ControllerType.SMC304:
                            ControllerConfig = new SmcIO();
                            break;

                        case ControllerType.EMC_E3064_A08:
                            ControllerConfig = new EmcIO();
                            break;
                        default:

                            break;
                    }
                    if (ControllerConfig != null)
                        ControllerConfig.DataContext = this;
                    break;
            }
        }

        private UserControl _ControllerConfig;
        /// <summary>
        /// Controller配置
        /// </summary>
        public UserControl ControllerConfig
        {
            get { return _ControllerConfig; }
            set { SetProperty(ref _ControllerConfig, value); }
        }

        private UserControl _AxisPointsConfig;
        /// <summary>
        /// 轴Points配置
        /// </summary>
        public UserControl AxisPointsConfig
        {
            get { return _AxisPointsConfig; }
            set { SetProperty(ref _AxisPointsConfig, value); }
        }

        private UserControl _InterpolationConfig;
        /// <summary>
        /// 插补配置
        /// </summary>
        public UserControl InterpolationConfig
        {
            get { return _InterpolationConfig; }
            set { SetProperty(ref _InterpolationConfig, value); }
        }
        #endregion Controller

        #region Axis

        /// <summary>
        /// Axes
        /// </summary>
        public ObservableCollection<Axis> Axes { get; set; } = [];
        private DelegateCommand<object> _AxisCommand;
        /// <summary>
        /// 轴Command
        /// </summary>
        public DelegateCommand<object> AxisCommand =>
            _AxisCommand ??= new DelegateCommand<object>(ExecuteAxisCommand);

        void ExecuteAxisCommand(object param)
        {

            var cmd = param as string;

            switch (cmd)
            {
                case "PowerOn":
                    if (GlobalModel.ModeState.ModeEnum == ModeState.WorkMode.Manual)
                    {


                        SelectedController.PowerOn(SelectedAxis.AxisNo);

                    }
                    else
                    {
                        SendInfoDialog("非手动模式禁止操作");
                    }
                    break;
                case "PowerOff":
                    if (GlobalModel.ModeState.ModeEnum == ModeState.WorkMode.Manual)
                    {
                        if (SelectedAxis.Moving)
                        {
                            SendInfoDialog("运动中禁止操作");
                        }
                        else
                        {
                            SelectedController.PowerOff(SelectedAxis.AxisNo);

                        }
                    }
                    else
                    {
                        SendInfoDialog("非手动模式禁止操作");
                    }
                    break;
                case "CreateORGDebug":
                    SelectedAxis.ORGCreated = true;
                    break;
                case "AbsoluteMotion":
                    SelectedAxis.AbsoluteMotion();
                    break;
                case "CreateORG":
                    SelectedAxis.CreateORG();
                    break;
                case "Stop":
                    SelectedController.Stop(SelectedAxis.AxisNo);

                    SelectedAxis.ErrorMessage = "";
                    break;
                case "+":
                    if (GlobalModel.ModeState.ModeEnum == ModeState.WorkMode.Manual)
                    {
                        SelectedAxis.RelativeDistance = Math.Abs(SelectedAxis.RelativeDistance);
                        SelectedAxis.Params.TargetDistance = SelectedAxis.RelativeDistance;
                        SelectedAxis.RelativeMotion();
                    }
                    else
                    {
                        SendInfoDialog("非手动模式禁止操作");
                    }
                    break;
                case "-":
                    if (GlobalModel.ModeState.ModeEnum == ModeState.WorkMode.Manual)
                    {
                        SelectedAxis.RelativeDistance = Math.Abs(SelectedAxis.RelativeDistance);
                        SelectedAxis.Params.TargetDistance = -SelectedAxis.RelativeDistance;
                        SelectedAxis.RelativeMotion();
                    }
                    else
                    {
                        SendInfoDialog("非手动模式禁止操作");
                    }

                    break;
                case "Jog0.01":
                    SelectedAxis.RelativeDistance = 0.01;
                    break;
                case "Jog0.1":
                    SelectedAxis.RelativeDistance = 0.1;
                    break;
                case "Jog1":
                    SelectedAxis.RelativeDistance = 1;
                    break;
                case "Jog5":
                    SelectedAxis.RelativeDistance = 5;
                    break;
                case "Jog10":
                    SelectedAxis.RelativeDistance = 10;
                    break;
                case "Jog20":
                    SelectedAxis.RelativeDistance = 20;
                    break;
                case "Save":
                    ExecuteAxisManage("Save");
                    SelectedController.Save(SelectedAxis.AxisNo);
                    break;

            }
        }

        private double _RelativeDistance;
        /// <summary>
        /// RelativeDistance
        /// </summary>
        public double RelativeDistance
        {
            get { return _RelativeDistance; }
            set { SetProperty(ref _RelativeDistance, value); }
        }

        private DelegateCommand<object> _DistanceSet;
        /// <summary>
        /// Distance设置
        /// </summary>
        public DelegateCommand<object> DistanceSet =>
            _DistanceSet ??= new DelegateCommand<object>(ExecuteDistanceSet);

        void ExecuteDistanceSet(object param)
        {
            var cmd = param as string;
            switch (cmd)
            {

                case "Jog0.01":
                    RelativeDistance = 0.01;
                    break;
                case "Jog0.1":
                    RelativeDistance = 0.1;
                    break;
                case "Jog1":
                    RelativeDistance = 1;
                    break;
                case "Jog5":
                    RelativeDistance = 5;
                    break;
                case "Jog10":
                    RelativeDistance = 10;
                    break;
                case "Jog20":
                    RelativeDistance = 20;
                    break;
            }
        }

        private Axis _SelectedAxis;
        /// <summary>
        /// Selected轴
        /// </summary>
        public Axis SelectedAxis
        {
            get { return _SelectedAxis; }
            set
            {
                SetProperty(ref _SelectedAxis, value);
                AxisConfig = null;
                switch (SelectedController.Type)
                {

                    case ControllerType.SMC304:
                        AxisConfig = new SmcAxis();
                        break;
                    case ControllerType.EMC_E3064_A08:

                        //case ControllerType.EMC_E3064_A12:

                        //case ControllerType.EMC_E3064_A16:

                        //case ControllerType.EMC_E3064_A24:

                        //case ControllerType.EMC_E3064_A32:

                        //case ControllerType.EMC_E3064_A64:

                        //case ControllerType.EMC_E5064_A08:

                        //case ControllerType.EMC_E5064_A16:

                        //case ControllerType.EMC_E5064_A24:

                        //case ControllerType.EMC_E5064_A32:

                        //case ControllerType.EMC_E5064_A64:
                        AxisConfig = new EmcAxis();
                        break;
                }
                if (AxisConfig != null)
                    AxisConfig.DataContext = this;

            }
        }
        private UserControl _AxisConfig;
        /// <summary>
        /// 轴配置
        /// </summary>
        public UserControl AxisConfig
        {
            get { return _AxisConfig; }
            set { SetProperty(ref _AxisConfig, value); }
        }

        private DelegateCommand<object> _AxisManage;
        /// <summary>
        /// 轴管理
        /// </summary>
        public DelegateCommand<object> AxisManage =>
            _AxisManage ??= new DelegateCommand<object>(ExecuteAxisManage);

        void ExecuteAxisManage(object param)
        {
            if (SelectedController == null)
            {
                SendInfoDialog("请先选择控制器！");
                return;
            }
            var cmd = param as string;
            switch (cmd)
            {
                case "New":
                    {
                        var axis = new Axis
                        {
                            ControllerID = SelectedController.ID,
                            ControllerNumber = SelectedController.ControllerNo
                        };
                        SelectedController.Axes.Add(axis);
                        Axes.Add(axis);
                    }
                    break;

                case "Save":
                    var names = new List<string>();

                    foreach (var item in SelectedController.Axes)
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
                    foreach (var item in SelectedController.Axes)
                    {
                        if (!_DatasContext.Axes.Any(h => h.ID == item.ID))
                        {
                            _DatasContext.Axes.Add(item);
                        }
                        else
                        {
                            var axis = _DatasContext.Axes.Where(c => c.ID == item.ID).FirstOrDefault();
                            axis.ControllerID = item.ControllerID;
                            axis.ControllerNumber = item.ControllerNumber;
                            axis.Name = item.Name;
                            axis.AxisNo = item.AxisNo;
                            axis.SerializedParams = item.SerializedParams;
                        }
                    }
                    _DatasContext.Save();
                    SelectedAxis.Prompt = "";
                    break;

                case "Remove":
                    if (SelectedAxis != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除轴 [{SelectedAxis.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;

                        var removedb = _DatasContext.Axes.Where(h => h.ID == SelectedAxis.ID).FirstOrDefault();
                        if (removedb != null)
                            _DatasContext.Axes.Remove(removedb);


                        var remove = SelectedController.Axes.Where(h => h.ID == SelectedAxis.ID).FirstOrDefault();

                        if (remove != null)
                        {
                            SelectedController.Axes.Remove(remove);
                            Axes.Remove(remove);
                            var name = remove.Name;
                            SendInfoDialog($"已删除：{name}");
                            _DatasContext.Save();
                        }

                    }

                    break;
            }
        }
        #endregion Axis

        #region AxisPoint
        private ObservableCollection<AxisPoint> _AxisPoints = [];
        /// <summary>
        /// 轴Points
        /// </summary>
        public ObservableCollection<AxisPoint> AxisPoints
        {
            get { return _AxisPoints; }
            set { SetProperty(ref _AxisPoints, value); }
        }

        private AxisPoint _SelectedAxisPoint;
        /// <summary>
        /// Selected轴点
        /// </summary>
        public AxisPoint SelectedAxisPoint
        {
            get { return _SelectedAxisPoint; }
            set { SetProperty(ref _SelectedAxisPoint, value); }
        }

        private DelegateCommand<object> _AxisPointsManage;
        /// <summary>
        /// 轴Points管理
        /// </summary>
        public DelegateCommand<object> AxisPointsManage =>
            _AxisPointsManage ??= new DelegateCommand<object>(ExecuteAxisPointsManage);

        void ExecuteAxisPointsManage(object param)
        {
            var cmd = param as string;
            switch (cmd)
            {
                case "New":
                    {
                        var axisPoint = new AxisPoint
                        {
                            RecipeID = GlobalModel.CurrentRecipe.ID,
                        };
                        AxisPoints.Add(axisPoint);
                    }
                    break;

                case "Save":
                    var names = new List<string>();

                    foreach (var item in AxisPoints)
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
                    foreach (var item in AxisPoints)
                    {
                        if (!_DatasContext.AxisPoints.Any(h => h.ID == item.ID))
                        {
                            _DatasContext.AxisPoints.Add(item);
                        }
                        else
                        {
                            var axisPoint = _DatasContext.AxisPoints.Where(c => c.ID == item.ID).FirstOrDefault();
                            axisPoint.X = item.X;
                            axisPoint.Y = item.Y;
                            axisPoint.Z = item.Z;
                            axisPoint.U = item.U;
                            axisPoint.SerializedParams = item.SerializedParams;
                        }
                    }
                    _DatasContext.Save();
                    break;

                case "Remove":
                    if (SelectedAxisPoint != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除点位 [{SelectedAxisPoint.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;

                        var removedb = _DatasContext.AxisPoints.Where(h => h.ID == SelectedAxisPoint.ID).FirstOrDefault();
                        if (removedb != null)
                        {
                            _DatasContext.AxisPoints.Remove(removedb);
                            _DatasContext.Save();
                        }
                        var remove = AxisPoints.Where(h => h.ID == SelectedAxisPoint.ID).FirstOrDefault();

                        if (remove != null)
                        {
                            AxisPoints.Remove(remove);
                            var name = remove.Name;
                            SendInfoDialog($"已删除：{name}");

                        }

                    }

                    break;

                case "Jump":
                    SelectedAxisPoint?.Jump();
                    break;
                case "Go":

                    _dialogService.Show("AlertDialog", new DialogParameters($"message=choose:直接运行至此点吗？"), r =>
                    {
                        if (r.Result == ButtonResult.Yes)
                        {
                            SelectedAxisPoint?.Go();

                        }

                    });
                    break;

            }
        }

        private DelegateCommand<object> _AxisPointCommand;
        /// <summary>
        /// 轴点位Command
        /// </summary>
        public DelegateCommand<object> AxisPointCommand =>
            _AxisPointCommand ??= new DelegateCommand<object>(ExecuteAxisPointCommand);

        void ExecuteAxisPointCommand(object param)
        {
            if (SelectedAxisPoint == null) return;
            var cmd = param as string;
            switch (cmd)
            {
                case "X+":
                    if (SelectedAxisPoint.AxisX == null) return;
                    SelectedAxisPoint.AxisX.Params.Rate = SelectedAxisPoint.Rate * SelectedAxisPoint.XRate / 100;

                    SelectedAxisPoint.AxisX.Params.TargetDistance = RelativeDistance;
                    SelectedAxisPoint.AxisX.RelativeMotion();
                    break;
                case "X-":
                    if (SelectedAxisPoint.AxisX == null) return;
                    SelectedAxisPoint.AxisX.Params.Rate = SelectedAxisPoint.Rate * SelectedAxisPoint.XRate / 100;

                    SelectedAxisPoint.AxisX.Params.TargetDistance = -RelativeDistance;
                    SelectedAxisPoint.AxisX.RelativeMotion();
                    break;
                case "Y+":
                    if (SelectedAxisPoint.AxisY == null) return;
                    SelectedAxisPoint.AxisY.Params.Rate = SelectedAxisPoint.Rate * SelectedAxisPoint.YRate / 100;

                    SelectedAxisPoint.AxisY.Params.TargetDistance = RelativeDistance;
                    SelectedAxisPoint.AxisY.RelativeMotion();
                    break;
                case "Y-":
                    if (SelectedAxisPoint.AxisY == null) return;
                    SelectedAxisPoint.AxisY.Params.Rate = SelectedAxisPoint.Rate * SelectedAxisPoint.YRate / 100;

                    SelectedAxisPoint.AxisY.Params.TargetDistance = -RelativeDistance;
                    SelectedAxisPoint.AxisY.RelativeMotion();
                    break;
                case "Z+":
                    if (SelectedAxisPoint.AxisZ == null) return;
                    SelectedAxisPoint.AxisZ.Params.Rate = SelectedAxisPoint.Rate * SelectedAxisPoint.ZRate / 100;

                    SelectedAxisPoint.AxisZ.Params.TargetDistance = RelativeDistance;
                    SelectedAxisPoint.AxisZ.RelativeMotion();
                    break;
                case "Z-":
                    if (SelectedAxisPoint.AxisZ == null) return;
                    SelectedAxisPoint.AxisZ.Params.Rate = SelectedAxisPoint.Rate * SelectedAxisPoint.ZRate / 100;

                    SelectedAxisPoint.AxisZ.Params.TargetDistance = -RelativeDistance;
                    SelectedAxisPoint.AxisZ.RelativeMotion();
                    break;
                case "U+":
                    if (SelectedAxisPoint.AxisU == null) return;
                    SelectedAxisPoint.AxisU.Params.Rate = SelectedAxisPoint.Rate * SelectedAxisPoint.URate / 100;

                    SelectedAxisPoint.AxisU.Params.TargetDistance = RelativeDistance;
                    SelectedAxisPoint.AxisU.RelativeMotion();
                    break;
                case "U-":
                    if (SelectedAxisPoint.AxisU == null) return;
                    SelectedAxisPoint.AxisU.Params.Rate = SelectedAxisPoint.Rate * SelectedAxisPoint.URate / 100;

                    SelectedAxisPoint.AxisU.Params.TargetDistance = -RelativeDistance;
                    SelectedAxisPoint.AxisU.RelativeMotion();
                    break;
                case "Save":
                    SelectedAxisPoint.Save();

                    break;
                case "Stop":
                    SelectedAxisPoint?.Stop();
                    break;
            }

        }
        #endregion Point

        #region Matrix
        private ObservableCollection<Matrix> _Matrices = [];
        /// <summary>
        /// 矩阵列表
        /// </summary>
        public ObservableCollection<Matrix> Matrices
        {
            get { return _Matrices; }
            set { SetProperty(ref _Matrices, value); }
        }

        private Matrix _SelectedMatrix;
        /// <summary>
        /// 
        /// </summary>
        public Matrix SelectedMatrix
        {
            get { return _SelectedMatrix; }
            set { SetProperty(ref _SelectedMatrix, value); }
        }


        private DelegateCommand<object> _MatricesManage;
        public DelegateCommand<object> MatricesManage =>
            _MatricesManage ??= new DelegateCommand<object>(ExecuteMatricesManage);
        void ExecuteMatricesManage(object param)
        {
            var cmd = param as string;
            switch (cmd)
            {
                case "New":
                    {
                        var matrix = new Matrix
                        {
                            RecipeID = GlobalModel.CurrentRecipe.ID,
                        };
                        Matrices.Add(matrix);
                    }
                    break;
                case "Create":
                    if (SelectedMatrix != null)
                    {
                        Create(SelectedMatrix);
                    }

                    break;

                case "Save":
                    var names = new List<string>();

                    foreach (var item in Matrices)
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
                    foreach (var item in Matrices)
                    {
                        if (!_DatasContext.Matrices.Any(h => h.ID == item.ID))
                        {
                            _DatasContext.Matrices.Add(item);
                        }
                        else
                        {
                            var matrix = _DatasContext.Matrices.Where(c => c.ID == item.ID).FirstOrDefault();
                            matrix.Name = item.Name;
                            matrix.StartName = item.StartName;
                            matrix.XEndName = item.XEndName;
                            matrix.YEndName = item.YEndName;
                            matrix.XCount = item.XCount;
                            matrix.YCount = item.YCount;
                            matrix.MatrixType = item.MatrixType;


                        }
                    }
                    _DatasContext.Save();
                    break;

                case "Remove":
                    if (SelectedMatrix != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除矩阵 [{SelectedMatrix.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;

                        var removedb = _DatasContext.Matrices.Where(h => h.ID == SelectedMatrix.ID).FirstOrDefault();
                        if (removedb != null)
                        {
                            _DatasContext.Matrices.Remove(removedb);
                            _DatasContext.Save();
                        }
                        var remove = Matrices.Where(h => h.ID == SelectedMatrix.ID).FirstOrDefault();

                        if (remove != null)
                        {
                            Matrices.Remove(remove);
                            var name = remove.Name;
                            SendInfoDialog($"已删除矩阵：{name}");

                        }

                    }

                    break;

                case "Jump":
                    var pointJump = SelectedMatrix.GetPoint(SelectedMatrix.XTarget, SelectedMatrix.YTarget);

                    if (pointJump == null)
                    {

                        SendInfoDialog("目标点不存在，请检查目标设置");
                        return;

                    }
                    pointJump.Jump();

                    break;
                case "Go":
                    if (SelectedMatrix == null) return;

                    var point = SelectedMatrix.GetPoint(SelectedMatrix.XTarget, SelectedMatrix.YTarget);

                    if (point == null)
                    {

                        SendInfoDialog("目标点不存在，请检查目标设置");
                        return;

                    }

                    _dialogService.Show("AlertDialog", new DialogParameters($"message=choose:直接运行至此点吗？"), r =>
                    {
                        if (r.Result == ButtonResult.Yes)
                        {

                            point.Go();
                        }

                    });
                    break;
            }
        }
        internal void Create(Matrix matrix)
        {
            matrix.Points.Clear();
            matrix.XCount = matrix.XCount > 0 ? matrix.XCount : 1;
            matrix.YCount = matrix.YCount > 0 ? matrix.YCount : 1;
            int xCount = matrix.XCount;
            int yCount = matrix.YCount;
            int total = xCount * yCount;

            // 从 AxisPoints 中查找三个角点
            var startPoint = AxisPoints.FirstOrDefault(p => p.Name == matrix.StartName);
            var xEndPoint = AxisPoints.FirstOrDefault(p => p.Name == matrix.XEndName);
            var yEndPoint = AxisPoints.FirstOrDefault(p => p.Name == matrix.YEndName);

            if (startPoint == null || xEndPoint == null || yEndPoint == null)
            {

                SendErr("基准点位名称错误，创建失败！");
                return;
            }

            // 计算 X/Y 方向基向量（当 XCount/YCount 为 1 时间距为 0）
            double xBasisX = xCount > 1 ? (xEndPoint.X - startPoint.X) / (xCount - 1) : 0;
            double xBasisY = xCount > 1 ? (xEndPoint.Y - startPoint.Y) / (xCount - 1) : 0;
            double yBasisX = yCount > 1 ? (yEndPoint.X - startPoint.X) / (yCount - 1) : 0;
            double yBasisY = yCount > 1 ? (yEndPoint.Y - startPoint.Y) / (yCount - 1) : 0;
            double zBasisX = xCount > 1 ? (xEndPoint.Z - startPoint.Z) / (xCount - 1) : 0;
            double zBasisY = yCount > 1 ? (yEndPoint.Z - startPoint.Z) / (yCount - 1) : 0;

            double originX = startPoint.X;
            double originY = startPoint.Y;
            double originZ = startPoint.Z;

            // 按 MatrixType 确定遍历顺序
            // 0 = 先X后Y (外层Y, 内层X); 1 = 先Y后X (外层X, 内层Y)
            for (int i = 0; i < total; i++)
            {
                int x, y;
                if (matrix.MatrixType == 0)
                {
                    // 先X后Y: X变快, Y变慢
                    x = i % xCount;
                    y = i / xCount;
                }
                else
                {
                    // 先Y后X: Y变快, X变慢
                    y = i % yCount;
                    x = i / yCount;
                }

                // 平行四边形插值坐标：P = start + x*xBasis + y*yBasis
                double px = originX + x * xBasisX + y * yBasisX;
                double py = originY + x * xBasisY + y * yBasisY;
                double pz = originZ + x * zBasisX + y * zBasisY;

                var point = new AxisPoint
                {
                    XIndex = x,
                    YIndex = y,
                    X = px,
                    Y = py,
                    Z = pz,
                    // 复制起始点的轴配置
                    AxisX = startPoint.AxisX,
                    AxisY = startPoint.AxisY,
                    AxisZ = startPoint.AxisZ,
                    AxisU = startPoint.AxisU,
                    U = startPoint.U
                };

                matrix.Points.Add(point);
            }
        }
        #endregion

        #region Interpolation

        /// <summary>
        /// 插补Groups
        /// </summary>
        public ObservableCollection<InterpolationGroup> InterpolationGroups { get; set; } = [];

        private InterpolationGroup _SelectedInterpolationGroup;
        /// <summary>
        /// Selected插补Group
        /// </summary>
        public InterpolationGroup SelectedInterpolationGroup
        {
            get { return _SelectedInterpolationGroup; }
            set { SetProperty(ref _SelectedInterpolationGroup, value); }
        }
        private DelegateCommand<object> _InterpolationsGroupManage;
        /// <summary>
        /// InterpolationsGroup管理
        /// </summary>
        public DelegateCommand<object> InterpolationsGroupManage =>
            _InterpolationsGroupManage ??= new DelegateCommand<object>(ExecuteInterpolationGroupsManage);

        void ExecuteInterpolationGroupsManage(object param)
        {
            var cmd = param as string;
            switch (cmd)
            {
                case "New":
                    {
                        var interpolationGroup = new InterpolationGroup()
                        {
                            RecipeID = GlobalModel.CurrentRecipe.ID,

                        };
                        InterpolationGroups.Add(interpolationGroup);
                    }
                    break;

                case "Remove":
                    if (SelectedInterpolationGroup != null)
                    {
                        if (System.Windows.MessageBox.Show("确认删除插补组？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        var group = _DatasContext.InterpolationGroups.FirstOrDefault(h => h.ID == SelectedInterpolationGroup.ID);
                        if (group != null)
                            _DatasContext.InterpolationGroups.Remove(group);
                        InterpolationGroups.Remove(SelectedInterpolationGroup);
                        _DatasContext.Save();
                    }
                    break;

                case "Save":
                    var names = new List<string>();

                    foreach (var item in InterpolationGroups)
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
                    foreach (var item in InterpolationGroups)
                    {
                        if (!_DatasContext.InterpolationGroups.Any(h => h.ID == item.ID))
                        {
                            _DatasContext.InterpolationGroups.Add(item);
                        }
                        else
                        {
                            var interpolation = _DatasContext.InterpolationGroups.Where(c => c.ID == item.ID).FirstOrDefault();
                            interpolation.Name = item.Name;
                            interpolation.AxisXName = item.AxisXName;
                            interpolation.AxisYName = item.AxisYName;
                            interpolation.SerializedParams = item.SerializedParams;
                            interpolation.SerializedInterpolations = item.SerializedInterpolations;

                        }

                    }
                    _DatasContext.Save();
                    break;




                case "Go":

                    _dialogService.Show("AlertDialog", new DialogParameters($"message=choose:确认可安全运行"), r =>
                    {
                        if (r.Result == ButtonResult.Yes)
                        {
                            SelectedInterpolationGroup?.Go();
                        }

                    });
                    break;


            }
        }

        private Interpolation _SelectedInterpolation;
        /// <summary>
        /// Selected插补
        /// </summary>
        public Interpolation SelectedInterpolation
        {
            get { return _SelectedInterpolation; }
            set { SetProperty(ref _SelectedInterpolation, value); }
        }
        private DelegateCommand<object> _InterpolationsManage;
        /// <summary>
        /// Interpolations管理
        /// </summary>
        public DelegateCommand<object> InterpolationsManage =>
            _InterpolationsManage ??= new DelegateCommand<object>(ExecuteInterpolationsManage);

        void ExecuteInterpolationsManage(object param)
        {
            var cmd = param as string;
            switch (cmd)
            {
                case "NewLine":
                    {
                        var interpolation = new Interpolation
                        {
                            Type = InterpolationType.Line直线,

                        };
                        interpolation.Params.PositionMode = 1;
                        interpolation.InterpolationPoints.Add(new() { Name = "终点" });
                        SelectedInterpolationGroup.Interpolations.Add(interpolation);
                        InterpolationsGroupManage.Execute("Save");
                    }
                    break;
                case "NewArc":
                    {
                        var interpolation = new Interpolation
                        {
                            Type = InterpolationType.Arc圆弧,
                        };
                        interpolation.Params.PositionMode = 1;
                        interpolation.InterpolationPoints.Add(new() { Name = "圆心" });
                        interpolation.InterpolationPoints.Add(new() { Name = "终点" });
                        SelectedInterpolationGroup.Interpolations.Add(interpolation);
                        InterpolationsGroupManage.Execute("Save");
                    }
                    break;
                case "Remove":
                    if (SelectedInterpolationGroup != null)
                    {
                        if (SelectedInterpolation != null)
                        {
                            if (System.Windows.MessageBox.Show("确认删除插补？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                break;
                            SelectedInterpolationGroup.Interpolations.Remove(SelectedInterpolation);

                            InterpolationsGroupManage.Execute("Save");
                        }
                    }

                    break;
                case "Save":
                    InterpolationsGroupManage.Execute("Save");
                    break;

                case "MoveUp":
                    if (SelectedInterpolation != null
                        && SelectedInterpolationGroup.Interpolations.IndexOf(SelectedInterpolation) > 0)
                    {

                        var currentIndex = SelectedInterpolationGroup.Interpolations.IndexOf(SelectedInterpolation);
                        SelectedInterpolationGroup.Interpolations.Move(currentIndex, currentIndex - 1);
                        InterpolationsGroupManage.Execute("Save");

                    }

                    break;

                case "MoveDown":
                    if (SelectedInterpolation != null
                        && SelectedInterpolationGroup.Interpolations.IndexOf(SelectedInterpolation)
                            < SelectedInterpolationGroup.Interpolations.Count - 1)
                    {

                        var currentIndex = SelectedInterpolationGroup.Interpolations.IndexOf(SelectedInterpolation);
                        SelectedInterpolationGroup.Interpolations.Move(currentIndex, currentIndex + 1);
                        InterpolationsGroupManage.Execute("Save");
                    }


                    break;
            }
        }

        private InterpolationPoint _SelectedInterpolationPoint;
        /// <summary>
        /// Selected插补点
        /// </summary>
        public InterpolationPoint SelectedInterpolationPoint
        {
            get { return _SelectedInterpolationPoint; }
            set { SetProperty(ref _SelectedInterpolationPoint, value); }
        }

        private DelegateCommand<object> _InterpolationPointCommand;
        /// <summary>
        /// 插补点位Command
        /// </summary>
        public DelegateCommand<object> InterpolationPointCommand =>
            _InterpolationPointCommand ??= new DelegateCommand<object>(ExecuteInterpolationPointCommand);

        void ExecuteInterpolationPointCommand(object param)
        {

            var cmd = param as string;
            switch (cmd)
            {
                case "X+":
                    if (SelectedInterpolationGroup.AxisX == null) return;
                    SelectedInterpolationGroup.AxisX.Params.Rate = 100;

                    SelectedInterpolationGroup.AxisX.Params.TargetDistance = RelativeDistance;
                    SelectedInterpolationGroup.AxisX.RelativeMotion();
                    break;
                case "X-":
                    if (SelectedInterpolationGroup.AxisX == null) return;
                    SelectedInterpolationGroup.AxisX.Params.Rate = 100;

                    SelectedInterpolationGroup.AxisX.Params.TargetDistance = -RelativeDistance;
                    SelectedInterpolationGroup.AxisX.RelativeMotion();
                    break;
                case "Y+":
                    if (SelectedInterpolationGroup.AxisY == null) return;
                    SelectedInterpolationGroup.AxisY.Params.Rate = 100;

                    SelectedInterpolationGroup.AxisY.Params.TargetDistance = RelativeDistance;
                    SelectedInterpolationGroup.AxisY.RelativeMotion();
                    break;
                case "Y-":
                    if (SelectedInterpolationGroup.AxisY == null) return;
                    SelectedInterpolationGroup.AxisY.Params.Rate = 100;
                    SelectedInterpolationGroup.AxisY.Params.TargetDistance = -RelativeDistance;
                    SelectedInterpolationGroup.AxisY.RelativeMotion();
                    break;

                case "Save":
                    if (SelectedInterpolationPoint == null) return;
                    if (SelectedInterpolationGroup.AxisX == null) return;
                    SelectedInterpolationPoint.X = SelectedInterpolationGroup.AxisX.CommandPosition;
                    if (SelectedInterpolationGroup.AxisY == null) return;
                    SelectedInterpolationPoint.Y = SelectedInterpolationGroup.AxisY.CommandPosition;
                    var temp = SelectedInterpolation;
                    InterpolationsGroupManage.Execute("Save");
                    SelectedInterpolation = temp;

                    break;
                case "Stop":
                    SelectedInterpolationGroup?.Stop();
                    break;

                case "Go":
                    if (SelectedInterpolationGroup.AxisX == null) return;
                    if (SelectedInterpolationGroup.AxisY == null) return;

                    SelectedInterpolationGroup.AxisX.Params.Rate = 100;
                    SelectedInterpolationGroup.AxisX.Params.MaxVelocity
                       = SelectedInterpolationGroup.Params.MaxVelocity;
                    SelectedInterpolationGroup.AxisX.Params.TargetPos
                        = SelectedInterpolationPoint.X;
                    SelectedInterpolationGroup.AxisX.AbsoluteMotion();


                    SelectedInterpolationGroup.AxisY.Params.Rate = 100;
                    SelectedInterpolationGroup.AxisY.Params.MaxVelocity
                       = SelectedInterpolationGroup.Params.MaxVelocity;
                    SelectedInterpolationGroup.AxisY.Params.TargetPos
                        = SelectedInterpolationPoint.Y;
                    SelectedInterpolationGroup.AxisY.AbsoluteMotion();
                    break;
            }

        }

        internal void LoadRecipe(Guid CurrentRecipeID)
        {


            Stop();
            AxisPoints.Clear();
            var currRecipeAxisPoints = _DatasContext.AxisPoints.Where(c => c.RecipeID == CurrentRecipeID);
            foreach (var item in currRecipeAxisPoints)
            {
                var axisPoint = item.DeepCopy();
                AxisPoints.Add(axisPoint);
                var axisX = Axes.Where(a => a.Name == axisPoint.AxisXName).FirstOrDefault();
                if (axisX != null)
                    axisPoint.AxisX = axisX;
                var axisY = Axes.Where(a => a.Name == axisPoint.AxisYName).FirstOrDefault();
                if (axisY != null)
                    axisPoint.AxisY = axisY;
                var axisZ = Axes.Where(a => a.Name == axisPoint.AxisZName).FirstOrDefault();
                if (axisZ != null)
                    axisPoint.AxisZ = axisZ;
                var axisU = Axes.Where(a => a.Name == axisPoint.AxisUName).FirstOrDefault();
                if (axisU != null)
                    axisPoint.AxisU = axisU;
            }
            //加载当前配方的矩阵列表
            Matrices.Clear();
            var currRecipeMatrices = _DatasContext.Matrices.Where(c => c.RecipeID == CurrentRecipeID);
            foreach (var item in currRecipeMatrices)
            {
                var matrix = item.DeepCopy();
                Matrices.Add(matrix);

                Create(matrix);


            }

            //加载当前配方的插补列表
            InterpolationGroups.Clear();
            var currRecipeInterpolationGroups = _DatasContext.InterpolationGroups.Where(c => c.RecipeID == CurrentRecipeID);
            foreach (var item in currRecipeInterpolationGroups)
            {
                var itemcopy = item.DeepCopy();
                InterpolationGroups.Add(itemcopy);
                var axisX = Axes.Where(a => a.Name == itemcopy.AxisXName).FirstOrDefault();
                if (axisX != null)
                    itemcopy.AxisX = axisX;
                var axisY = Axes.Where(a => a.Name == itemcopy.AxisYName).FirstOrDefault();
                if (axisY != null)
                    itemcopy.AxisY = axisY;
            }
            Start();

        }



        #endregion
    }
}

