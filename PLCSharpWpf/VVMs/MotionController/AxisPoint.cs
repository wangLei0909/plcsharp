using Newtonsoft.Json;
using PLCSharp.Core.Common;
using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// 轴点位
    /// </summary>
    public class AxisPoint : BindableBase
    {
        #region 属性
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
        /// 点名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private double _X;
        /// <summary>
        /// X坐标
        /// </summary>
        public double X
        {
            get { return _X; }
            set { SetProperty(ref _X, value); }
        }

        private double _Y;
        /// <summary>
        /// Y
        /// </summary>
        public double Y
        {
            get { return _Y; }
            set { SetProperty(ref _Y, value); }
        }

        private double _Z;
        /// <summary>
        /// Z
        /// </summary>
        public double Z
        {
            get { return _Z; }
            set { SetProperty(ref _Z, value); }
        }

        private double _U;
        /// <summary>
        /// U
        /// </summary>
        public double U
        {
            get { return _U; }
            set { SetProperty(ref _U, value); }
        }

        private Axis _AxisX;
        /// <summary>
        /// 轴X
        /// </summary>
        [NotMapped]
        public Axis AxisX
        {
            get { return _AxisX; }
            set
            {
                SetProperty(ref _AxisX, value);
                if (value != null)
                    AxisXName = value.Name;
                else
                    AxisXName = string.Empty;

            }
        }
        private string _AxisXName;
        /// <summary>
        /// 轴X名称
        /// </summary>
        public string AxisXName
        {
            get { return _AxisXName; }
            set { SetProperty(ref _AxisXName, value); }
        }
        private Axis _AxisY;
        /// <summary>
        /// 轴Y
        /// </summary>
        [NotMapped]
        public Axis AxisY
        {
            get { return _AxisY; }
            set
            {
                SetProperty(ref _AxisY, value);
                if (value != null)
                    AxisYName = value.Name;
                else
                    AxisYName = string.Empty;
            }
        }
        private string _AxisYName;
        /// <summary>
        /// 轴Y名称
        /// </summary>
        public string AxisYName
        {
            get { return _AxisYName; }
            set { SetProperty(ref _AxisYName, value); }
        }

        private Axis _AxisZ;
        /// <summary>
        /// 轴Z
        /// </summary>
        [NotMapped]
        public Axis AxisZ
        {
            get { return _AxisZ; }
            set
            {
                SetProperty(ref _AxisZ, value);
                if (value != null)
                    AxisZName = value.Name;
                else
                    AxisZName = string.Empty;
            }
        }
        private string _AxisZName;
        /// <summary>
        /// 轴Z名称
        /// </summary>
        public string AxisZName
        {
            get { return _AxisZName; }
            set { SetProperty(ref _AxisZName, value); }
        }

        private double _ZSafe;
        /// <summary>
        /// ZSafe
        /// </summary>
        public double ZSafe
        {
            get { return _ZSafe; }
            set { SetProperty(ref _ZSafe, value); }
        }

        private Axis _AxisU;
        /// <summary>
        /// 轴U
        /// </summary>
        [NotMapped]
        public Axis AxisU
        {
            get { return _AxisU; }
            set
            {
                SetProperty(ref _AxisU, value);
                if (value != null)
                    AxisUName = value.Name;
                else
                    AxisUName = string.Empty;
            }
        }

        private string _AxisUName;
        /// <summary>
        /// 轴U名称
        /// </summary>
        public string AxisUName
        {
            get { return _AxisUName; }
            set { SetProperty(ref _AxisUName, value); }
        }
        private double _XRate = 100;
        /// <summary>
        /// XRate
        /// </summary>
        public double XRate
        {
            get { return _XRate; }
            set
            {
                if (value > 100)
                    value = 100;
                else if (value < 1)
                    value = 1;
                SetProperty(ref _XRate, value);



            }
        }

        private double _YRate = 100;
        /// <summary>
        /// YRate
        /// </summary>
        public double YRate
        {
            get { return _YRate; }
            set
            {
                if (value > 100)
                    value = 100;
                else if (value < 1)
                    value = 1;
                SetProperty(ref _YRate, value);



            }
        }

        private double _ZRate = 100;
        /// <summary>
        /// ZRate
        /// </summary>
        public double ZRate
        {
            get { return _ZRate; }
            set
            {
                if (value > 100)
                    value = 100;
                else if (value < 1)
                    value = 1;
                SetProperty(ref _ZRate, value);



            }
        }
        private double _URate = 100;
        /// <summary>
        /// URate
        /// </summary>
        public double URate
        {
            get { return _URate; }
            set
            {
                if (value > 100)
                    value = 100;
                else if (value < 1)
                    value = 1;
                SetProperty(ref _URate, value);


            }
        }
        private double _Rate = 100;
        /// <summary>
        /// Rate
        /// </summary>
        [NotMapped]
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

        /// <summary>
        /// 序列化Params
        /// </summary>
        [Column("Params")]
        public string SerializedParams
        {
            get => JsonConvert.SerializeObject(Params); // 自动序列化
            set => Params = JsonConvert.DeserializeObject<AxisPointParams>(value); // 自动反序列化
        }

        private AxisPointParams _Params;
        /// <summary>
        /// 参数集合
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 配置项
        /// </summary>
        public AxisPointParams Params
        {
            get
            {
                _Params ??= new AxisPointParams();

                return _Params;
            }
            set
            {
                SetProperty(ref _Params, value);
            }
        }
        /// <summary>
        /// 轴点位Params
        /// </summary>
        public class AxisPointParams : BindableBase
        {


        }
        #endregion

        #region 方法
        private readonly FlowModel flowModel = new();
        /// <summary>
        /// 运行
        /// </summary>
        public void Run()
        {
            switch (flowModel.Step)
            {
                case 1:
                    if (AxisZ != null)
                    {
                        AxisZ.Params.TargetPos = ZSafe;
                        if (AxisZ.AbsoluteMotion())
                        {
                            flowModel.Step++;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        flowModel.Step = 3;
                    }
                    break;
                case 2:
                    if (AxisZ.AbsoluteDone)
                    {
                        flowModel.Step++;
                    }
                    break;
                case 3:
                    if (AxisX != null)
                    {
                        AxisX.Params.TargetPos = X;
                        if (AxisX.AbsoluteMotion())
                        {
                            flowModel.Step++;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        flowModel.Step++;
                    }
                    break;
                case 4:
                    if (AxisY != null)
                    {
                        AxisY.Params.TargetPos = Y;
                        if (AxisY.AbsoluteMotion())
                        {
                            flowModel.Step++;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        flowModel.Step++;
                    }
                    break;
                case 5:
                    if (AxisU != null)
                    {
                        AxisU.Params.TargetPos = U;
                        if (AxisU.AbsoluteMotion())
                        {
                            flowModel.Step++;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        flowModel.Step++;
                    }
                    break;

                case 6:
                    if (AxisX == null || AxisX.AbsoluteDone)
                    {
                        flowModel.Step++;
                    }
                    break;
                case 7:
                    if (AxisY == null || AxisY.AbsoluteDone)
                    {
                        flowModel.Step++;
                    }
                    break;
                case 8:
                    if (AxisU == null || AxisU.AbsoluteDone)
                    {
                        flowModel.Step++;
                    }
                    break;
                case 9:
                    if (AxisZ != null)
                    {
                        AxisZ.Params.TargetPos = Z;
                        if (AxisZ.AbsoluteMotion())
                        {
                            flowModel.Step++;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        flowModel.Step++;
                    }
                    break;
                case 10:
                    if (AxisZ == null || AxisZ.AbsoluteDone)
                    {
                        flowModel.Step = 0;
                        flowModel.Done = true;
                    }
                    break;

                case 11:
                    if (AxisX != null)
                    {
                        AxisX.Params.TargetPos = X;
                        if (AxisX.AbsoluteMotion())
                        {
                            flowModel.Step++;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        flowModel.Step++;
                    }
                    break;
                case 12:
                    if (AxisY != null)
                    {
                        AxisY.Params.TargetPos = Y;
                        if (AxisY.AbsoluteMotion())
                        {
                            flowModel.Step++;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        flowModel.Step++;
                    }
                    break;
                case 13:
                    if (AxisU != null)
                    {
                        AxisU.Params.TargetPos = U;
                        if (AxisU.AbsoluteMotion())
                        {
                            flowModel.Step++;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        flowModel.Step++;
                    }
                    break;
                case 14:
                    if (AxisZ != null)
                    {
                        AxisZ.Params.TargetPos = Z;
                        if (AxisZ.AbsoluteMotion())
                        {
                            flowModel.Step++;
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        flowModel.Step++;
                    }
                    break;
                case 15:
                    if (AxisX == null || AxisX.AbsoluteDone)
                    {
                        flowModel.Step++;
                    }
                    break;
                case 16:
                    if (AxisY == null || AxisY.AbsoluteDone)
                    {
                        flowModel.Step++;
                    }
                    break;
                case 17:
                    if (AxisU == null || AxisU.AbsoluteDone)
                    {
                        flowModel.Step++;
                    }
                    break;

                case 18:
                    if (AxisZ == null || AxisZ.AbsoluteDone)
                    {
                        flowModel.Step = 0;
                        flowModel.Done = true;
                    }
                    break;

            }

        }
        /// <summary>
        /// Jump
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool Jump()
        {
            if (AxisX is not null) { AxisX.Params.Rate = _Rate * _XRate / 100; }
            if (AxisY is not null) { AxisY.Params.Rate = _Rate * _YRate / 100; }
            if (AxisZ is not null) { AxisZ.Params.Rate = _Rate * _ZRate / 100; }
            if (AxisU is not null) { AxisU.Params.Rate = _Rate * _URate / 100; }
            flowModel.Done = false;
            flowModel.Step = 1;
            return true;
        }
        /// <summary>
        /// Go
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool Go()
        {
            if (AxisX is not null) { AxisX.Params.Rate = _Rate * _XRate / 100; }
            if (AxisY is not null) { AxisY.Params.Rate = _Rate * _YRate / 100; }
            if (AxisZ is not null) { AxisZ.Params.Rate = _Rate * _ZRate / 100; }
            if (AxisU is not null) { AxisU.Params.Rate = _Rate * _URate / 100; }
            flowModel.Done = false;
            flowModel.Step = 11;
            return true;
        }


        /// <summary>
        /// IsDone
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool IsDone()
        {
            return flowModel.Done;
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            flowModel.Reset();

            AxisX?.Stop();
            AxisY?.Stop();
            AxisZ?.Stop();
            AxisU?.Stop();
        }

        /// <summary>
        /// 保存
        /// </summary>
        internal void Save()
        {
            if (AxisX != null) X = AxisX.CommandPosition;
            if (AxisY != null) Y = AxisY.CommandPosition;
            if (AxisZ != null) Z = AxisZ.CommandPosition;
            if (AxisU != null) U = AxisU.CommandPosition;
        }
        #endregion
    }
}
