using Newtonsoft.Json;
using PLCSharp.Core.Tools;
using PLCSharp.Models;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
using System.Windows;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// Controller
    /// </summary>
    public class Controller : BindableBase
    {
        private static DateTime _lastConnErrLog = DateTime.MinValue;
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        private ushort _ControllerNo;
        /// <summary>
        /// ControllerNo
        /// </summary>
        public ushort ControllerNo
        {
            get { return _ControllerNo; }
            set { SetProperty(ref _ControllerNo, value); }
        }
        private ControllerType _Type;
        /// <summary>
        /// 类型
        /// </summary>
        public ControllerType Type
        {
            get { return _Type; }
            set
            {

                if (_Type == ControllerType.Undefined)
                {
                    if (value != ControllerType.Undefined)
                    {
                        SetProperty(ref _Type, value);

                    }
                }
                else if (_Type != value)
                {
                    MessageBox.Show("选定型号后不可改变！");
                }
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {


            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;

            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();

        }
        /// <summary>
        /// Axes
        /// </summary>
        [NotMapped]
        public ObservableCollection<Axis> Axes { get; set; } = [];
        /// <summary>
        /// DI
        /// </summary>
        [NotMapped]
        public ObservableCollection<DI> DI { get; set; } = [];
        /// <summary>
        /// DQ
        /// </summary>
        [NotMapped]
        public ObservableCollection<DQ> DQ { get; set; } = [];

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;


            while (!worker.CancellationPending)
            {


                Thread.Sleep(1);

                if (_Type == ControllerType.Undefined)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (SDK == null)
                {
                    switch (_Type)
                    {
                        case ControllerType.SMC304:
                            SDK = new SMC();
                            break;

                        case ControllerType.EMC_E3064_A08:

                            //case ControllerType.EMC_E3064_A16:

                            //case ControllerType.EMC_E3064_A12:

                            //case ControllerType.EMC_E3064_A24:

                            //case ControllerType.EMC_E3064_A32:

                            //case ControllerType.EMC_E3064_A64:

                            //case ControllerType.EMC_E5064_A08:

                            //case ControllerType.EMC_E5064_A12:

                            //case ControllerType.EMC_E5064_A16:

                            //case ControllerType.EMC_E5064_A24:

                            //case ControllerType.EMC_E5064_A32:

                            //case ControllerType.EMC_E5064_A64:
                            SDK = new EMC();

                            break;
                        default:
                            MessageBox.Show("未定义的控制器类型，请检查代码！");
                            _Type = ControllerType.Undefined;
                            break;

                    }
                }


                if (Connected == false)
                {
                    Thread.Sleep(1000);
                    Online = NetTool.PingIP(IP);

                    if (Online)
                    {

                        Connected = SDK.Init(ControllerNo, IP);
                        if (Connected)
                        {

                        }
                        else
                        {
                            if ((DateTime.Now - _lastConnErrLog).TotalSeconds > 10)
                            {
                                _lastConnErrLog = DateTime.Now;
                                GlobalModel.SendErr("连接失败，请检查IP地址或控制器是否在线");
                            }
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {

                        Connected = false;
                        if ((DateTime.Now - _lastConnErrLog).TotalSeconds > 10)
                        {
                            _lastConnErrLog = DateTime.Now;
                            GlobalModel.SendErr("连接失败，请检查IP地址或控制器是否在线");
                        }
                        Thread.Sleep(1000);

                    }
                }
                else
                {

                    AxesRun();
                    IORun();

                }

            }


        }

        private void IORun()
        {

            if (DI.Count > 0)
            {
                SDK.DI_Count = DI.Count;
                SDK.GetDI();


                for (int i = 0; i < DI.Count; i++)
                {
                    var group = i / 32;
                    var bit = i % 32;
                    DI[i].Status = !SDK.DI[group].GetBit(bit);

                }
            }
            if (DQ.Count > 0)
            {
                SDK.DQ_Count = DQ.Count;
                SDK.GetDQ();
                for (int i = 0; i < DQ.Count; i++)
                {
                    var group = i / 32;
                    var bit = i % 32;
                    DQ[i].Status = !SDK.DQ[group].GetBit(bit);
                }
            }

        }
        /// <summary>
        /// 设置DQ
        /// </summary>
        /// <param name="port">端口</param>
        /// <param name="off_on">off_on</param>
        public void SetDQ(ushort port, ushort off_on)
        {
            SDK.SetDQ(port, off_on);
        }

        private void AxesRun()
        {
            for (int i = 0; i < Axes.Count; i++)
            {
                SDK.Get_Axis_Status(Axes[i].AxisNo);
                if (SDK.AxisIOs.Count >= Axes.Count)
                {
                    Axes[i].ORG = SDK.AxisIOs[Axes[i].AxisNo].ORG;
                    Axes[i].PowerOn = SDK.AxisIOs[Axes[i].AxisNo].PowerOn;
                    Axes[i].ALM = SDK.AxisIOs[Axes[i].AxisNo].ALM;
                    Axes[i].LimitN = SDK.AxisIOs[Axes[i].AxisNo].LimitN;
                    Axes[i].LimitP = SDK.AxisIOs[Axes[i].AxisNo].LimitP;
                    Axes[i].Moving = SDK.AxisIOs[Axes[i].AxisNo].Moving;
                    Axes[i].CommandPosition = SDK.AxisIOs[Axes[i].AxisNo].CommandPosition;
                    Axes[i].EncoderPosition = SDK.AxisIOs[Axes[i].AxisNo].EncoderPosition;
                    Axes[i].Velocity = SDK.AxisIOs[Axes[i].AxisNo].Velocity;
                }
                else
                {


                }

                if (GlobalModel.ModeState.Mode.Production)
                {
                    if (Axes[i].PowerOn == false)
                    {

                        SDK.Power(Axes[i].AxisNo, true);

                    }


                }

            }



            //运动流程
            for (int i = 0; i < Axes.Count; i++)
            {
                var axis = Axes[i];
                if (axis.MotionFlow.Step > 0)
                {
                    switch (axis.MotionFlow.Step)
                    {
                        case 1:

                            axis.ErrCode = SDK.CreateORG(axis.AxisNo, axis.Params);
                            if (axis.ErrCode != 0)
                            {
                                axis.MotionFlow.Step = 0;
                            }
                            else
                            {
                                axis.MotionFlow.Step++;
                            }
                            break;
                        case 2:
                            if (SDK.CheckORG(axis.AxisNo))
                            {
                                axis.ORGCreated = true;
                                axis.MotionFlow.Step = 0;
                            }
                            break;
                        case 11:

                            axis.ErrCode = SDK.Move(axis.AxisNo, 1, axis.Params);
                            if (axis.ErrCode != 0)
                            {
                                axis.MotionFlow.Step = 0;
                            }
                            else
                            {
                                axis.MotionFlow.Step++;
                            }
                            break;
                        case 12:
                            if (axis.Moving == false)
                            {
                                if (axis.CommandPosition == axis.Params.TargetPos)
                                {
                                    axis.AbsoluteDone = true;
                                }
                                axis.MotionFlow.Step = 0;
                            }
                            break;

                        case 21:

                            axis.ErrCode = SDK.Move(axis.AxisNo, 0, axis.Params);
                            if (axis.ErrCode != 0)
                            {
                                axis.MotionFlow.Step = 0;
                            }
                            else
                            {
                                axis.MotionFlow.Step++;
                            }
                            break;
                        case 22:
                            if (axis.Moving == false)
                            {

                                axis.RelativeDone = true;

                                axis.MotionFlow.Step = 0;
                            }
                            break;
                        case 31:
                            axis.ErrCode = SDK.MulticoorMove(axis.Interpolations, axis.InterpolationGroupParams);
                            if (axis.ErrCode != 0)
                            {
                                axis.MotionFlow.Step = 0;
                            }
                            else
                            {
                                axis.MotionFlow.Step++;
                            }
                            break;
                        case 32:
                            axis.MotionFlow.Step = 0;
                            break;
                        case 101:
                            Stop(axis.AxisNo);
                            break;
                    }
                }
            }
        }

        private ControllerSDK SDK;
        private BackgroundWorker bkgWorker;
        private string _IP;
        /// <summary>
        /// IP
        /// </summary>
        public string IP
        {
            get { return _IP; }
            set { SetProperty(ref _IP, value); }
        }
        private string _Comment;
        /// <summary>
        /// 注释
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
            get => JsonConvert.SerializeObject(Params); // 自动序列化
            set => Params = JsonConvert.DeserializeObject<ControllerParams>(value); // 自动反序列化

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
        private bool _Online;

        /// <summary>
        /// Online
        /// </summary>
        [NotMapped]
        public bool Online
        {
            get { return _Online; }
            set { SetProperty(ref _Online, value); }
        }
        private bool _Connected;

        /// <summary>
        /// Connected
        /// </summary>
        [NotMapped]
        public bool Connected
        {
            get { return _Connected; }
            set { SetProperty(ref _Connected, value); }
        }
        private void ParamsChanged()
        {
            Prompt = "已修改，请保存";
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public virtual void Close()
        {
            SDK?.Close();

        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="ControllerNo">ControllerNo</param>
        /// <param name="ip">ip</param>
        /// <returns>返回布尔值</returns>
        public virtual bool Init(ushort ControllerNo, string ip)
        {
            return false;

        }
        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="axisNo">轴No</param>
        internal void Stop(ushort axisNo)
        {
            Axes[axisNo].MotionFlow.Step = 0;
            SDK.Stop(axisNo);
            if (Axes[axisNo].ALM)
            {
                SDK.Reset(axisNo);
            }
        }

        /// <summary>
        /// PowerOn
        /// </summary>
        /// <param name="axisNo">轴No</param>
        internal void PowerOn(ushort axisNo)
        {

            Axes[axisNo].MotionFlow.Step = 0;

            SDK.Power(axisNo, true);



        }
        /// <summary>
        /// PowerOff
        /// </summary>
        /// <param name="axisNo">轴No</param>
        internal void PowerOff(ushort axisNo)
        {

            Axes[axisNo].MotionFlow.Step = 0;

            SDK.Power(axisNo, false);



        }
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="axisNo">轴No</param>
        internal void Save(ushort axisNo)
        {
            SDK.Save(axisNo, Axes[axisNo].Params);
        }

        private ControllerParams _Params;
        /// <summary>
        /// 参数集合
        /// </summary>
        [NotMapped]
        public ControllerParams Params
        {
            get
            {
                _Params ??= new ControllerParams();
                return _Params;
            }
            set
            {
                SetProperty(ref _Params, value);
            }
        }


        /// <summary>
        /// ControllerParams
        /// </summary>
        public class ControllerParams : BindableBase
        {

            private string _Prompt;
            /// <summary>
            /// 提示
            /// </summary>
            [JsonIgnore]
            public string Prompt
            {
                get { return _Prompt; }
                set { SetProperty(ref _Prompt, value); }
            }

        }
        /// <summary>
        /// 全局模型
        /// </summary>
        [NotMapped]
        public GlobalModel GlobalModel { get; set; }


    }


}
