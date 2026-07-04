using PLCSharp.Core.Prism;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Robots
{
    /// <summary>
    /// Robots视图模型
    /// </summary>
    public class RobotsViewModel : DialogAwareBase
    {
        /// <summary>
        /// Robots视图模型
        /// </summary>
        public RobotsViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            RobotModel = container.Resolve<RobotModel>();

        }


        /// <summary>
        /// 机器人模型
        /// </summary>
        public RobotModel RobotModel { get; set; }

        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {

        }
        /// <summary>
        /// 关闭对话框后要执行的
        /// </summary>
        public override void OnDialogClosed()
        {

        }


        #region Robot Point CRUD
        private Robot _SelectedRobot;
        /// <summary>
        /// Selected机器人
        /// </summary>
        public Robot SelectedRobot
        {
            get { return _SelectedRobot; }
            set
            {
                SetProperty(ref _SelectedRobot, value);

                switch (_SelectedRobot.Type)
                {
                    case RobotType.Undefined:
                        break;
                    case RobotType.Epson:
                        RobotPointsConfig = new Epson.RobotPointsConfig
                        {
                            DataContext = this
                        };
                        break;
                    default:
                        break;
                }


            }
        }



        private DelegateCommand<object> _RobotPointManage;
        /// <summary>
        /// 机器人点位管理
        /// </summary>
        public DelegateCommand<object> RobotPointManage =>
            _RobotPointManage ??= new DelegateCommand<object>(ExecuteRobotPointManage);

        void ExecuteRobotPointManage(object param)
        {
            var cmd = param as string;
            switch (cmd)
            {
                case "New":
                    if (SelectedRobot == null)
                    {
                        SendInfoDialog("请选择机器人");
                        return;
                    }
                    var newPoint = new RobotPoint
                    {
                        RobotID = SelectedRobot.ID,
                        RecipeID = RobotModel.GlobalModel?.CurrentRecipe?.ID ?? Guid.Empty,
                        Name = "新点位",
                        Rate = 100
                    };
                    SelectedRobot.Points.Add(newPoint);
                    SelectedRobot.SelectedPoint = newPoint;
                    break;

                case "Save":
                    var names = new List<string>();
                    foreach (var item in SelectedRobot.Points)
                    {
                        if (string.IsNullOrEmpty(item.Name))
                        {
                            SendInfoDialog("保存失败，点位名称不能为空");
                            return;
                        }
                        if (names.Contains(item.Name))
                        {
                            SendInfoDialog($"保存失败，重复名称：{item.Name}");
                            return;
                        }
                        names.Add(item.Name);

                        if (!RobotModel._DatasContext.RobotPoints.Any(h => h.ID == item.ID))
                        {
                            RobotModel._DatasContext.RobotPoints.Add(item);
                        }
                        else
                        {
                            var exist = RobotModel._DatasContext.RobotPoints.FirstOrDefault(p => p.ID == item.ID);
                            if (exist != null)
                            {
                                exist.Name = item.Name;
                                exist.X = item.X;
                                exist.Y = item.Y;
                                exist.Z = item.Z;
                                exist.U = item.U;

                                exist.Rate = item.Rate;

                                exist.Hand = item.Hand;
                                exist.ToolNum = item.ToolNum;
                                exist.UF = item.UF;

                                exist.PointType = item.PointType;

                            }
                        }
                    }
                    RobotModel._DatasContext.Save();
                    SendInfoDialog("点位保存成功");
                    break;

                case "Remove":
                    if (SelectedRobot.SelectedPoint != null)
                    {
                        if (RobotModel._DatasContext.RobotPoints.Any(h => h.ID == SelectedRobot.SelectedPoint.ID))
                        {
                            var remove = RobotModel._DatasContext.RobotPoints.FirstOrDefault(p => p.ID == SelectedRobot.SelectedPoint.ID);
                            if (remove != null)
                                RobotModel._DatasContext.RobotPoints.Remove(remove);
                        }
                        SelectedRobot.Points.Remove(SelectedRobot.SelectedPoint);
                        RobotModel._DatasContext.Save();
                        SelectedRobot.SelectedPoint = null;
                    }
                    break;


            }
        }
        #endregion
        #region Robot CRUD




        private DelegateCommand<string> _RobotManage;
        /// <summary>
        /// 机器人管理
        /// </summary>
        public DelegateCommand<string> RobotManage =>
            _RobotManage ??= new DelegateCommand<string>(ExecuteRobotManage);

        void ExecuteRobotManage(string cmd)
        {
            switch (cmd)
            {
                case "New":
                    var robot = new Robot();
                    RobotModel.Robots.Add(robot);
                    break;

                case "Save":
                    foreach (var item in RobotModel.Robots)
                    {
                        if (!RobotModel._DatasContext.Robots.Any(h => h.ID == item.ID))
                        {
                            RobotModel._DatasContext.Robots.Add(item);

                        }
                        else
                        {
                            var exist = RobotModel._DatasContext.Robots.FirstOrDefault(c => c.ID == item.ID);
                            if (exist != null)
                            {
                                exist.Name = item.Name;
                                exist.Type = item.Type;
                                exist.IP = item.IP;
                                exist.Port = item.Port;
                                exist.CommanPort = item.CommanPort;
                                exist.Speed = item.Speed;
                                exist.Comment = item.Comment;
                                exist.SerializedParams = item.SerializedParams;
                                exist.Tools = item.Tools;
                            }
                        }
                    }
                    RobotModel._DatasContext.Save();
                    if (SelectedRobot != null)
                        SelectedRobot.Prompt = "";
                    SendInfoDialog("机器人保存成功");
                    break;

                case "Remove":
                    if (SelectedRobot != null)
                    {
                        if (RobotModel._DatasContext.Robots.Any(h => h.ID == SelectedRobot.ID))
                        {
                            var remove = RobotModel._DatasContext.Robots.FirstOrDefault(h => h.ID == SelectedRobot.ID);
                            if (remove != null)
                                RobotModel._DatasContext.Robots.Remove(remove);
                        }
                        RobotModel.Robots.Remove(SelectedRobot);
                        RobotModel._DatasContext.Save();
                        SendInfoDialog("已删除");
                    }
                    break;


            }
        }



        #endregion


        #region 机器人参数
        private double _RelativeDistance = 0.1;
        /// <summary>
        /// 相对移动距离
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

        void ExecuteDistanceSet(object distObj)
        {
            string distStr = distObj as string;

            if (double.TryParse(distStr, out double dist))
            {
                RelativeDistance = dist;
            }

        }



        private UserControl _RobotPointsConfig;
        /// <summary>
        /// 机器人Points配置
        /// </summary>
        public UserControl RobotPointsConfig
        {
            get { return _RobotPointsConfig; }
            set { SetProperty(ref _RobotPointsConfig, value); }
        }

        private DelegateCommand<object> _RobotPointCommand;
        /// <summary>
        /// 机器人点位Command
        /// </summary>
        public DelegateCommand<object> RobotPointCommand =>
            _RobotPointCommand ??= new DelegateCommand<object>(ExecuteRobotPointCommand);

        void ExecuteRobotPointCommand(object cmd)
        {

            var cmdStr = cmd as string;
            if (SelectedRobot.SelectedPoint == null)
            {
                SendInfoDialog("请选择点位");
                return;
            }
            SelectedRobot.SelectedPoint.Safe = true;
            switch (cmdStr)

            {
                case "RunPoint":

                    SelectedRobot.RunPoint(SelectedRobot.SelectedPoint);
                    break;


                case "X+":
                case "X-":

                case "Y+":
                case "Y-":

                case "Z+":
                case "Z-":

                case "U+":
                case "U-":
                    SelectedRobot.CurrPoint.ToolNum = SelectedRobot.SelectedPoint.ToolNum;
                    SelectedRobot.Jog(SelectedRobot.CurrPoint, cmdStr, RelativeDistance);
                    break;
            }


        }


        private DelegateCommand _SaveRobotPointCommand;
        /// <summary>
        /// 保存机器人点位Command
        /// </summary>
        public DelegateCommand SaveRobotPointCommand =>
            _SaveRobotPointCommand ??= new DelegateCommand(ExecuteSaveRobotPointCommand);

        void ExecuteSaveRobotPointCommand()
        {
            if (SelectedRobot.CurrPoint != null && SelectedRobot.SelectedPoint != null)
            {

                SelectedRobot.SelectedPoint.X = SelectedRobot.CurrPoint.X;
                SelectedRobot.SelectedPoint.Y = SelectedRobot.CurrPoint.Y;
                SelectedRobot.SelectedPoint.Z = SelectedRobot.CurrPoint.Z;
                SelectedRobot.SelectedPoint.U = SelectedRobot.CurrPoint.U;

                var point = RobotModel._DatasContext.RobotPoints.FirstOrDefault(p => p.ID == SelectedRobot.SelectedPoint.ID);

                if (point != null)
                {
                    point.X = SelectedRobot.SelectedPoint.X;
                    point.Y = SelectedRobot.SelectedPoint.Y;
                    point.Z = SelectedRobot.SelectedPoint.Z;
                    point.U = SelectedRobot.SelectedPoint.U;
                }

                RobotModel._DatasContext.Save();

            }
        }
        #endregion


        #region 机器人坐标系

        private DelegateCommand _GetToolFrame4Axis;
        /// <summary>
        /// 获取工具Frame4轴
        /// </summary>
        public DelegateCommand GetToolFrame4Axis =>
            _GetToolFrame4Axis ??= new DelegateCommand(ExecuteGetToolFrame4Axis);

        void ExecuteGetToolFrame4Axis()
        {
            if (SelectedRobot.Params.ToolP1 == null)
            {
                SendInfoDialog("P1未设置");
                return;


            }
            if (SelectedRobot.Params.ToolP2 == null)
            {
                SendInfoDialog("P2未设置");
                return;


            }
            if (SelectedRobot.ToolTarget < 1)
            {
                SendInfoDialog("机器人目标<1");
                return;


            }
            try
            {
                SelectedRobot.Tools[SelectedRobot.ToolTarget] = ToolFrame4Axis.GenerateFromTwoPoints(SelectedRobot.Params.ToolP1, SelectedRobot.Params.ToolP2);
                var robot = RobotModel._DatasContext.Robots.FirstOrDefault(f => f.ID == SelectedRobot.ID);
                robot.Tools = SelectedRobot.Tools;
                RobotModel._DatasContext.Save();
                SendInfoDialog("设置成功");
            }
            catch (Exception ex)
            {
                SendInfoDialog(ex.Message);

            }

        }

        private DelegateCommand _SetToolP1;
        /// <summary>
        /// 设置工具P1
        /// </summary>
        public DelegateCommand SetToolP1 =>
            _SetToolP1 ??= new DelegateCommand(ExecuteSetToolP1);

        void ExecuteSetToolP1()
        {
            SelectedRobot.Params.ToolP1 = new()
            {
                X = SelectedRobot.CurrPoint.X,
                Y = SelectedRobot.CurrPoint.Y,
                U = SelectedRobot.CurrPoint.U
            };
        }

        private DelegateCommand _SetToolP2;
        /// <summary>
        /// 设置工具P2
        /// </summary>
        public DelegateCommand SetToolP2 =>
            _SetToolP2 ??= new DelegateCommand(ExecuteSetToolP2);

        void ExecuteSetToolP2()
        {
            SelectedRobot.Params.ToolP2 = new()
            {
                X = SelectedRobot.CurrPoint.X,
                Y = SelectedRobot.CurrPoint.Y,
                U = SelectedRobot.CurrPoint.U
            };

        }
        #endregion
    }
}
