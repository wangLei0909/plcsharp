using Newtonsoft.Json;
using PLCSharp.Core.Common;
using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PLCSharp.VVMs.MotionController.InterpolationGroup;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// 轴
    /// </summary>
    public class Axis : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
        /// <summary>
        /// ControllerID
        /// </summary>
        public Guid ControllerID { get; set; }

        private ushort _ControllerNumber;
        /// <summary>
        /// 所属的控制序号
        /// </summary>
        public ushort ControllerNumber
        {
            get { return _ControllerNumber; }
            set { SetProperty(ref _ControllerNumber, value); }
        }

        private string _Name;
        /// <summary>
        /// 点名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private ushort _AxisNo;
        /// <summary>
        /// 轴No
        /// </summary>
        public ushort AxisNo
        {
            get { return _AxisNo; }
            set { SetProperty(ref _AxisNo, value); }
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

        private string _Comment;
        /// <summary>
        /// 备注
        /// </summary>
        public string Comment
        {
            get { return _Comment; }
            set { SetProperty(ref _Comment, value); }
        }


        private bool _ALM;
        /// <summary>
        /// ALM
        /// </summary>
        [NotMapped]
        public bool ALM
        {
            get { return _ALM; }
            set { SetProperty(ref _ALM, value); }
        }

        private bool _ORG;
        /// <summary>
        /// ORG
        /// </summary>
        [NotMapped]
        public bool ORG
        {
            get { return _ORG; }
            set { SetProperty(ref _ORG, value); }
        }
        private bool _ORGCreated;
        /// <summary>
        /// ORGCreated
        /// </summary>
        [NotMapped]
        public bool ORGCreated
        {
            get { return _ORGCreated; }
            set { SetProperty(ref _ORGCreated, value); }
        }

        private bool _PowerOn;
        /// <summary>
        /// PowerOn
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 使能
        /// </summary>
        public bool PowerOn
        {
            get { return _PowerOn; }
            set { SetProperty(ref _PowerOn, value); }
        }
        /// <summary>
        /// 创建ORG
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool CreateORG()
        {
            if (Moving)
            {
                ErrorMessage = "运动中,请先停止";
                return false;
            }
            if (Interlock.Home)
            {
                ErrorMessage = "回原锁定";
                return false;
            }
            ORGCreated = false;
            MotionFlow.Step = 1;
            return true;
        }
        private bool _AbsoluteDone;
        /// <summary>
        /// AbsoluteDone
        /// </summary>
        [NotMapped]
        public bool AbsoluteDone
        {
            get { return _AbsoluteDone; }
            set { SetProperty(ref _AbsoluteDone, value); }
        }

        private bool _RelativeDone;
        /// <summary>
        /// RelativeDone
        /// </summary>
        [NotMapped]
        public bool RelativeDone
        {
            get { return _RelativeDone; }
            set { SetProperty(ref _RelativeDone, value); }
        }

        private bool _InterpolationDone;
        /// <summary>
        /// 插补Done
        /// </summary>
        [NotMapped]
        public bool InterpolationDone
        {
            get { return _InterpolationDone; }
            set { SetProperty(ref _InterpolationDone, value); }
        }
        private double _RelativeDistance;
        /// <summary>
        /// RelativeDistance
        /// </summary>
        [NotMapped]
        public double RelativeDistance
        {
            get { return _RelativeDistance; }
            set { SetProperty(ref _RelativeDistance, value); }
        }

        private AxisLock _Interlock = new();
        /// <summary>
        /// Interlock
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 安全联锁
        /// </summary>
        public AxisLock Interlock
        {
            get { return _Interlock; }
            set { SetProperty(ref _Interlock, value); }
        }

        private string _ErrorMessage = "";
        /// <summary>
        /// 错误Message
        /// </summary>
        [NotMapped]
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
            set
            {
                SetProperty(ref _ErrorMessage, value);
                Error = value.Length > 0;
            }
        }
        private bool _Error;
        /// <summary>
        /// 错误
        /// </summary>
        [NotMapped]
        public bool Error
        {
            get { return _Error; }
            set { SetProperty(ref _Error, value); }
        }



        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {

            MotionFlow.Step = 101;

        }
        /// <summary>
        /// 运动Flow
        /// </summary>
        [NotMapped]
        public FlowModel MotionFlow { get; set; } = new();

        /// <summary>
        /// Absolute运动
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool AbsoluteMotion()
        {
            if (Moving)
            {
                ErrorMessage = "运动中,请先停止";
                return false;
            }
            if (ORGCreated == false)
            {
                ErrorMessage = "请先回原点";
                return false;
            }

            if (Interlock.Forward && Params.TargetPos > CommandPosition)
            {
                ErrorMessage = "正方向锁定";
                return false;
            }
            if (Interlock.Backward && Params.TargetPos < CommandPosition)
            {
                ErrorMessage = "负方向锁定";
                return false;
            }
            AbsoluteDone = false;
            MotionFlow.Step = 11;
            return true;
        }


        /// <summary>
        /// Relative运动
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool RelativeMotion()
        {
            if (Moving)
            {
                ErrorMessage = "运动中,请先停止";
                return false;
            }
            if (Interlock.Forward && Params.TargetDistance > 0)
            {
                ErrorMessage = "正方向锁定";
                return false;
            }
            if (Interlock.Backward && Params.TargetDistance < 0)
            {
                ErrorMessage = "负方向锁定";
                return false;
            }
            RelativeDone = false;
            MotionFlow.Step = 21;
            return true;
        }
        private bool _LimitP;
        /// <summary>
        /// LimitP
        /// </summary>
        [NotMapped]
        public bool LimitP
        {
            get { return _LimitP; }
            set { SetProperty(ref _LimitP, value); }
        }

        private bool _LimitN;
        /// <summary>
        /// LimitN
        /// </summary>
        [NotMapped]
        public bool LimitN
        {
            get { return _LimitN; }
            set { SetProperty(ref _LimitN, value); }
        }
        private bool _Moving;
        /// <summary>
        /// Moving
        /// </summary>
        [NotMapped]
        public bool Moving
        {
            get { return _Moving; }
            set { SetProperty(ref _Moving, value); }
        }

        private int _ErrCode;
        /// <summary>
        /// ErrCode
        /// </summary>
        [NotMapped]
        public int ErrCode
        {
            get { return _ErrCode; }
            set { SetProperty(ref _ErrCode, value); }
        }

        private double _CommandPosition;
        /// <summary>
        /// Command位置
        /// </summary>
        [NotMapped]
        public double CommandPosition
        {
            get { return _CommandPosition; }
            set { SetProperty(ref _CommandPosition, value); }
        }
        private double _EncoderPosition;
        /// <summary>
        /// Encoder位置
        /// </summary>
        [NotMapped]
        public double EncoderPosition
        {
            get { return _EncoderPosition; }
            set { SetProperty(ref _EncoderPosition, value); }
        }
        private double _Velocity;
        /// <summary>
        /// Velocity
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 当前速度
        /// </summary>
        public double Velocity
        {
            get { return _Velocity; }
            set { SetProperty(ref _Velocity, value); }
        }


        /// <summary>
        /// 序列化Params
        /// </summary>
        [Column("Params")]
        public string SerializedParams
        {
            get => JsonConvert.SerializeObject(Params); // 自动序列化
            set => Params = JsonConvert.DeserializeObject<AxisParams>(value); // 自动反序列化

        }
        private AxisParams _Params;
        /// <summary>
        /// 参数集合
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 配置项
        /// </summary>
        public AxisParams Params
        {
            get
            {
                _Params ??= new AxisParams();

                return _Params;
            }
            set
            {
                SetProperty(ref _Params, value);
            }
        }
        /// <summary>
        /// 插补GroupParams
        /// </summary>
        [NotMapped]
        public InterpolationGroupParams InterpolationGroupParams { get; set; }

        /// <summary>
        /// Interpolations
        /// </summary>
        [NotMapped]
        public List<Interpolation> Interpolations { get; set; }

        /// <summary>
        /// 轴Params
        /// </summary>
        public class AxisParams : BindableBase
        {

            private double _HomeLowVelocity = 10;
            /// <summary>
            /// 回原低速
            /// </summary>
            public double HomeLowVelocity
            {
                get { return _HomeLowVelocity; }
                set { SetProperty(ref _HomeLowVelocity, value); }
            }

            private double _HomeHighVelocity = 100;
            /// <summary>
            /// 回原高速
            /// </summary>
            public double HomeHighVelocity
            {
                get { return _HomeHighVelocity; }
                set { SetProperty(ref _HomeHighVelocity, value); }
            }

            private double _Equiv = 1000;
            /// <summary>
            /// 脉冲当量
            /// </summary>
            public double Equiv
            {
                get { return _Equiv; }
                set { SetProperty(ref _Equiv, value); }
            }
            private ushort _PulseOutMode;
            /// <summary>
            /// PulseOutMode
            /// </summary>
            public ushort PulseOutMode
            {
                get { return _PulseOutMode; }
                set { SetProperty(ref _PulseOutMode, value); }
            }
            private double _MinVelocity;
            /// <summary>
            /// 最小Velocity
            /// </summary>
            public double MinVelocity
            {
                get { return _MinVelocity; }
                set { SetProperty(ref _MinVelocity, value); }
            }

            private double _MaxVelocity = 1000;
            /// <summary>
            /// 最大Velocity
            /// </summary>
            public double MaxVelocity
            {
                get { return _MaxVelocity; }
                set { SetProperty(ref _MaxVelocity, value); }
            }
            private double _Rate = 100;
            /// <summary>
            /// Rate
            /// </summary>
            [JsonIgnore]
            /// <summary>
            /// 速率 1-100
            /// </summary>
            public double Rate
            {
                get { return _Rate; }
                set
                {
                    if (value > 100)
                        value = 100;
                    else if (value < 1)
                        value = 1;
                    SetProperty(ref _Rate, value);
                }
            }
            private double _StopVelocity;
            /// <summary>
            /// 停止Velocity
            /// </summary>
            public double StopVelocity
            {
                get { return _StopVelocity; }
                set { SetProperty(ref _StopVelocity, value); }
            }
            private double _Acc = 0.1;
            /// <summary>
            /// 加速时间
            /// </summary>
            public double Acc
            {
                get { return _Acc; }
                set { SetProperty(ref _Acc, value); }
            }

            private double _Dec = 0.1;
            /// <summary>
            /// Dec
            /// </summary>
            public double Dec
            {
                get { return _Dec; }
                set { SetProperty(ref _Dec, value); }
            }
            private double _TargetPos;
            /// <summary>
            /// TargetPos
            /// </summary>
            public double TargetPos
            {
                get { return _TargetPos; }
                set { SetProperty(ref _TargetPos, value); }
            }

            private double _TargetDistance;
            /// <summary>
            /// TargetDistance
            /// </summary>
            public double TargetDistance
            {
                get { return _TargetDistance; }
                set { SetProperty(ref _TargetDistance, value); }
            }

            private ushort _OrgLogic;
            /// <summary>
            /// 原点电平
            /// </summary>
            public ushort OrgLogic
            {
                get { return _OrgLogic; }
                set { SetProperty(ref _OrgLogic, value); }
            }

            private ushort _ForwardLimitLogic;
            /// <summary>
            /// ForwardLimitLogic
            /// </summary>
            public ushort ForwardLimitLogic
            {
                get { return _ForwardLimitLogic; }
                set { SetProperty(ref _ForwardLimitLogic, value); }
            }
            private ushort _ForwardLimitEnable;
            /// <summary>
            /// ForwardLimit启用
            /// </summary>
            public ushort ForwardLimitEnable
            {
                get { return _ForwardLimitEnable; }
                set { SetProperty(ref _ForwardLimitEnable, value); }
            }
            private ushort _BackwardLimitLogic;
            /// <summary>
            /// BackwardLimitLogic
            /// </summary>
            public ushort BackwardLimitLogic
            {
                get { return _BackwardLimitLogic; }
                set { SetProperty(ref _BackwardLimitLogic, value); }
            }

            private ushort _BackwardLimitEnable;
            /// <summary>
            /// BackwardLimit启用
            /// </summary>
            public ushort BackwardLimitEnable
            {
                get { return _BackwardLimitEnable; }
                set { SetProperty(ref _BackwardLimitEnable, value); }
            }

            private ushort _ALMLogic;
            /// <summary>
            /// ALMLogic
            /// </summary>
            public ushort ALMLogic
            {
                get { return _ALMLogic; }
                set { SetProperty(ref _ALMLogic, value); }
            }

            private ushort _ALMEnable;
            /// <summary>
            /// ALM启用
            /// </summary>
            public ushort ALMEnable
            {
                get { return _ALMEnable; }
                set { SetProperty(ref _ALMEnable, value); }
            }

            private ushort _HomeDir;
            /// <summary>
            /// 0-负向、1-正向
            /// </summary>
            public ushort HomeDir
            {
                get { return _HomeDir; }
                set { SetProperty(ref _HomeDir, value); }
            }
            private ushort _HomeMode;
            /// <summary>
            /// 回原点方式
            /// </summary>
            public ushort HomeMode
            {
                get { return _HomeMode; }
                set { SetProperty(ref _HomeMode, value); }
            }
        }

        /// <summary>
        /// 轴Lock
        /// </summary>
        public class AxisLock : BindableBase
        {
            private bool _Home;
            /// <summary>
            /// 回零
            /// </summary>
            public bool Home
            {
                get { return _Home; }
                set { SetProperty(ref _Home, value); }
            }

            private bool _Forward;
            /// <summary>
            /// 正向
            /// </summary>
            public bool Forward
            {
                get { return _Forward; }
                set { SetProperty(ref _Forward, value); }
            }

            private bool _Backward;
            /// <summary>
            /// 反向
            /// </summary>
            public bool Backward
            {
                get { return _Backward; }
                set { SetProperty(ref _Backward, value); }
            }
        }
    }
}
