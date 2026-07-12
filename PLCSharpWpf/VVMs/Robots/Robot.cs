using Newtonsoft.Json;
using PLCSharp.Core.Prism;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows;

namespace PLCSharp.VVMs.Robots
{
    /// <summary>
    /// 机器人
    /// </summary>
    public class Robot : BindableBase
    {
        [NotMapped]

        public RobotModel Model { get; set; }


        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
        private string _Name;
        /// <summary>
        /// 机器人名称
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
        private RobotType _Type;
        /// <summary>
        /// 机器人品牌类型
        /// </summary>
        public RobotType Type
        {
            get { return _Type; }
            set
            {
                if (_Type == RobotType.Undefined)
                {
                    SetProperty(ref _Type, value);
                }
                else
                {
                    MessageBox.Show("选定型号后不可改变！");
                }
            }
        }

        private string _IP;
        /// <summary>
        /// IP地址
        /// </summary>
        public string IP
        {
            get { return _IP; }
            set { SetProperty(ref _IP, value); }
        }

        private int _Port;
        /// <summary>
        /// 控制器端口
        /// </summary>
        public int Port
        {
            get { return _Port; }
            set { SetProperty(ref _Port, value); }
        }

        private int _CommanPort;
        /// <summary>
        /// 指令交互端口
        /// </summary>
        public int CommanPort
        {
            get { return _CommanPort; }
            set { SetProperty(ref _CommanPort, value); }
        }

        private int _Speed = 100;
        /// <summary>
        /// 总速度百分比 1-100
        /// </summary>
        public int Speed
        {
            get { return _Speed; }
            set
            {
                if (value > 100) value = 100;
                if (value < 1) value = 1;
                SetProperty(ref _Speed, value);
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
        /// <summary>
        /// 序列化Params
        /// </summary>
        [Column("Params")]
        public string SerializedParams
        {
            get => JsonConvert.SerializeObject(Params);
            set => Params = JsonConvert.DeserializeObject<RobotParams>(value);

        }

        private string _Prompt;
        /// <summary>
        /// 提示
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 提示
        /// </summary>
        public string Prompt
        {
            get { return _Prompt; }
            set { SetProperty(ref _Prompt, value); }
        }


        private ObservableCollection<bool> _Status = [false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false];
        /// <summary>
        /// 状态
        /// </summary>
        [NotMapped]
        public ObservableCollection<bool> Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

        private ObservableCollection<string> _StatusInfo = ["", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""];
        /// <summary>
        /// 状态信息
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 状态信息
        /// </summary> 
        public ObservableCollection<string> StatusInfo
        {
            get { return _StatusInfo; }
            set { SetProperty(ref _StatusInfo, value); }
        }

        private RobotParams _Params;
        /// <summary>
        /// 参数集合
        /// </summary>
        [NotMapped]
        public RobotParams Params
        {
            get
            {
                _Params ??= new RobotParams();
                return _Params;
            }
            set
            {
                SetProperty(ref _Params, value);
            }
        }

        /// <summary>
        /// 机器人Params
        /// </summary>
        public class RobotParams : BindableBase
        {
            private RobotPoint _ToolP1;

            /// <summary>
            /// 工具P1
            /// </summary>
            public RobotPoint ToolP1
            {
                get { return _ToolP1; }
                set { SetProperty(ref _ToolP1, value); }
            }

            private RobotPoint _ToolP2;

            /// <summary>
            /// 工具P2
            /// </summary>
            public RobotPoint ToolP2
            {
                get { return _ToolP2; }
                set { SetProperty(ref _ToolP2, value); }
            }

        }


        private ObservableCollection<RobotPoint> _Points = [];
        /// <summary>
        /// 坐标点集合
        /// </summary>
        [NotMapped]
        public ObservableCollection<RobotPoint> Points
        {
            get { return _Points; }
            set { SetProperty(ref _Points, value); }
        }


        private ObservableDictionary<int, ToolFrame4Axis> _Tools = [];
        /// <summary>
        /// Tools
        /// </summary>
        [NotMapped]
        public ObservableDictionary<int, ToolFrame4Axis> Tools
        {
            get { return _Tools; }
            set { SetProperty(ref _Tools, value); }
        }
        /// <summary>
        /// 序列化Tools
        /// </summary>
        [Column("Tools")]
        public string SerializedTools
        {
            get
            {
                var str = JsonConvert.SerializeObject(Tools);

                return str;
            }
            set => Tools = value != null ? JsonConvert.DeserializeObject<ObservableDictionary<int, ToolFrame4Axis>>(value) : [];
        }

        private int _ToolTarget;
        /// <summary>
        /// 工具Target
        /// </summary>
        [NotMapped]
        public int ToolTarget
        {
            get { return _ToolTarget; }
            set { SetProperty(ref _ToolTarget, value); }
        }


        private RobotPoint _SelectedPoint;
        /// <summary>
        /// 选中点
        /// </summary>
        [NotMapped]
        public RobotPoint SelectedPoint
        {
            get { return _SelectedPoint; }
            set { SetProperty(ref _SelectedPoint, value); }
        }
        private RobotPoint _CurrPoint = new();
        /// <summary>
        /// Curr点位
        /// </summary>
        [NotMapped]
        public RobotPoint CurrPoint
        {
            get { return _CurrPoint; }
            set { SetProperty(ref _CurrPoint, value); }
        }

        private RobotPoint _CurrToolPoint = new();
        /// <summary>
        /// Curr工具点位
        /// </summary>
        [NotMapped]
        public RobotPoint CurrToolPoint
        {
            get { return _CurrToolPoint; }
            set { SetProperty(ref _CurrToolPoint, value); }
        }

        private int _CurrTool;
        /// <summary>
        /// Curr工具
        /// </summary>
        [NotMapped]
        public int CurrTool
        {
            get { return _CurrTool; }
            set { SetProperty(ref _CurrTool, value); }
        }

        private string _RcvShowInfo;
        /// <summary>
        /// Rcv显示信息
        /// </summary>
        [NotMapped]
        public string RcvShowInfo
        {
            get { return _RcvShowInfo; }
            set { SetProperty(ref _RcvShowInfo, value); }
        }


        private string _ConnStatus;
        /// <summary>
        /// Conn状态
        /// </summary>
        [NotMapped]
        public string ConnStatus
        {
            get { return _ConnStatus; }
            set { SetProperty(ref _ConnStatus, value); }
        }

        private string _ErrorInfo;
        /// <summary>
        /// 错误信息
        /// </summary>
        [NotMapped]
        public string ErrorInfo
        {
            get { return _ErrorInfo; }
            set { SetProperty(ref _ErrorInfo, value); }
        }


        private DelegateCommand _StartCmd;
        /// <summary>
        /// 启动Cmd
        /// </summary>
        public DelegateCommand StartCmd =>
            _StartCmd ??= new DelegateCommand(ExecuteStartCmd);

        void ExecuteStartCmd()
        {
            Start();
        }

        /// <summary>
        /// 启动
        /// </summary>
        public virtual void Start()
        {
            System.Diagnostics.Debug.WriteLine($"[{Name}] Start");

        }
        private DelegateCommand _ResetCmd;
        /// <summary>
        /// 重置Cmd
        /// </summary>
        public DelegateCommand ResetCmd =>
            _ResetCmd ??= new DelegateCommand(ExecuteResetCmd);

        void ExecuteResetCmd()
        {

        }
        /// <summary>
        /// 重置
        /// </summary>
        public virtual void Reset()
        {


        }

        private DelegateCommand _StopCmd;
        /// <summary>
        /// 停止Cmd
        /// </summary>
        public DelegateCommand StopCmd =>
            _StopCmd ??= new DelegateCommand(ExecuteStopCmd);

        void ExecuteStopCmd()
        {
            Stop();
        }

        /// <summary>
        /// 停止
        /// </summary>
        public virtual void Stop()
        {


        }
        /// <summary>
        /// 点位Command
        /// </summary>
        [NotMapped]
        public RobotPoint PointCommand { get; set; }

        /// <summary>
        /// 运行点位
        /// </summary>
        /// <param name="robotPoint">机器人点位</param>
        public virtual void RunPoint(string pointName)
        {


        }
        /// <summary>
        /// Jog
        /// </summary>
        /// <param name="point">点位</param>
        /// <param name="cmd">命令参数</param>
        /// <param name="dist">dist</param>
        /// <param name="rate">rate</param>
        /// <returns>返回布尔值</returns>
        public virtual void Jog(string pointName, string cmd, double dist, double rate = 0)
        {

        }

        /// <summary>
        /// 点位运行完成
        /// </summary>
        [NotMapped]

        public bool PointDone { get; set; }

        private DelegateCommand _HomeCmd;
        /// <summary>
        /// 回零Cmd
        /// </summary>
        public DelegateCommand HomeCmd =>
            _HomeCmd ??= new DelegateCommand(ExecuteHomeCmd);

        void ExecuteHomeCmd()
        {
            Home();
        }
        /// <summary>
        /// 回零
        /// </summary>
        public virtual void Home()
        {


        }

        private DelegateCommand<object> _PowerCmd;
        /// <summary>
        /// PowerCmd
        /// </summary>
        public DelegateCommand<object> PowerCmd =>
            _PowerCmd ??= new DelegateCommand<object>(ExecutePowerCmd);

        void ExecutePowerCmd(object cmd)
        {
            var cmdStr = cmd as string;

            Power(cmdStr);
        }


        /// <summary>
        /// Power
        /// </summary>
        /// <param name="cmd">命令参数</param>
        public virtual void Power(string cmd)
        {


        }


        #region RobotMatrix

        private ObservableCollection<RobotMatrix> _RobotMatrices = [];
        /// <summary>
        /// 机器人矩阵列表
        /// </summary>
        [NotMapped]
        public ObservableCollection<RobotMatrix> RobotMatrices
        {
            get { return _RobotMatrices; }
            set { SetProperty(ref _RobotMatrices, value); }
        }

        /// <summary>
        /// 根据矩阵配置生成点位网格（平行四边形插值）
        /// </summary>
        /// <param name="matrix">矩阵配置</param>
        internal void Create(RobotMatrix matrix)
        {
            matrix.Points.Clear();



            int xCount = matrix.XCount > 0 ? matrix.XCount : 1;
            int yCount = matrix.YCount > 0 ? matrix.YCount : 1;
            int total = xCount * yCount;

            // 从机器人的点位列表中查找三个角点
            var startPoint = Points.FirstOrDefault(p => p.Name == matrix.StartName);
            var xEndPoint = Points.FirstOrDefault(p => p.Name == matrix.XEndName);
            var yEndPoint = Points.FirstOrDefault(p => p.Name == matrix.YEndName);

            if (startPoint == null || xEndPoint == null || yEndPoint == null)
            {
                Model.SendInfoDialog("基准点位名称错误，创建失败！");
                return;
            }

            // 计算 X/Y/Z 方向基向量（平行四边形插值）
            double xBasisX = xCount > 1 ? (xEndPoint.X - startPoint.X) / (xCount - 1) : 0;
            double xBasisY = xCount > 1 ? (xEndPoint.Y - startPoint.Y) / (xCount - 1) : 0;
            double xBasisZ = xCount > 1 ? (xEndPoint.Z - startPoint.Z) / (xCount - 1) : 0;
            double yBasisX = yCount > 1 ? (yEndPoint.X - startPoint.X) / (yCount - 1) : 0;
            double yBasisY = yCount > 1 ? (yEndPoint.Y - startPoint.Y) / (yCount - 1) : 0;
            double yBasisZ = yCount > 1 ? (yEndPoint.Z - startPoint.Z) / (yCount - 1) : 0;

            double originX = startPoint.X;
            double originY = startPoint.Y;
            double originZ = startPoint.Z;

            // 按 MatrixType 确定遍历顺序
            for (int i = 0; i < total; i++)
            {
                int x, y;
                if (matrix.MatrixType == 0)
                {
                    x = i % xCount;
                    y = i / xCount;
                }
                else
                {
                    y = i % yCount;
                    x = i / yCount;
                }

                // 平行四边形插值坐标（X/Y/Z 线性插值，U/V/W 取自起始点）
                double px = originX + x * xBasisX + y * yBasisX;
                double py = originY + x * xBasisY + y * yBasisY;
                double pz = originZ + x * xBasisZ + y * yBasisZ;

                var point = new RobotPoint
                {
                    RobotID = matrix.RobotID,
                    XIndex = x,
                    YIndex = y,
                    X = px,
                    Y = py,
                    Z = pz,
                    U = startPoint.U,
                    V = startPoint.V,
                    W = startPoint.W,
                    Rate = startPoint.Rate,
                    Hand = startPoint.Hand,
                    ToolNum = startPoint.ToolNum,
                };

                matrix.Points.Add(point);
            }
        }

        public virtual void RunMatrixPoint(RobotMatrix matrix, int xTarget, int yTarget)
        {

        }

        #endregion


    }
}
