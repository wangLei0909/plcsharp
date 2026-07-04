using Newtonsoft.Json;
using PLCSharp.Core.Common;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// 插补Group
    /// </summary>
    public class InterpolationGroup : BindableBase
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
                {

                    AxisXName = value.Name;
                    Params.AxisXNo = value.AxisNo;
                }
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
                {

                    AxisYName = value.Name;
                    Params.AxisYNo = value.AxisNo;
                }
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


        /// <summary>
        /// 序列化Interpolations
        /// </summary>
        [Column("Interpolations")]
        public string SerializedInterpolations
        {
            get => JsonConvert.SerializeObject(Interpolations); // 自动序列化
            set
            {
                Interpolations = JsonConvert.DeserializeObject<ObservableCollection<Interpolation>>(value); // 自动反序列化
            }
        }

        private ObservableCollection<Interpolation> _Interpolations = [];
        /// <summary>
        /// Interpolations
        /// </summary>
        [NotMapped]
        public ObservableCollection<Interpolation> Interpolations
        {
            get { return _Interpolations; }
            set { SetProperty(ref _Interpolations, value); }
        }

        /// <summary>
        /// 序列化Params
        /// </summary>
        [Column("Params")]
        public string SerializedParams
        {
            get => JsonConvert.SerializeObject(Params); // 自动序列化
            set => Params = JsonConvert.DeserializeObject<InterpolationGroupParams>(value); // 自动反序列化
        }

        private InterpolationGroupParams _Params;
        /// <summary>
        /// 参数集合
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 配置项
        /// </summary>
        public InterpolationGroupParams Params
        {
            get
            {
                _Params ??= new InterpolationGroupParams();

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
        public class InterpolationGroupParams : BindableBase
        {

            private string _Prompt;
            /// <summary>
            /// 提示
            /// </summary>
            [JsonIgnore]
            /// <summary>
            /// 提示
            /// </summary>
            public string Prompt
            {
                get { return _Prompt; }
                set { SetProperty(ref _Prompt, value); }
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

            private double _StopVelocity;
            /// <summary>
            /// 停止Velocity
            /// </summary>
            public double StopVelocity
            {
                get { return _StopVelocity; }
                set { SetProperty(ref _StopVelocity, value); }
            }
            private ushort _Coordinate;
            /// <summary>
            /// 坐标系
            /// </summary>
            public ushort Coordinate
            {
                get { return _Coordinate; }
                set { SetProperty(ref _Coordinate, value); }
            }

            private double _Ahead;
            /// <summary>
            /// Ahead
            /// </summary>
            public double Ahead
            {
                get { return _Ahead; }
                set { SetProperty(ref _Ahead, value); }
            }

            /// <summary>
            /// 轴XNo
            /// </summary>
            [JsonIgnore]
            public ushort AxisXNo { get; set; }

            /// <summary>
            /// 轴YNo
            /// </summary>
            [JsonIgnore]
            public ushort AxisYNo { get; set; }


            private ushort _OutBit;
            /// <summary>
            /// OutBit
            /// </summary>
            public ushort OutBit
            {
                get { return _OutBit; }
                set { SetProperty(ref _OutBit, value); }
            }
            private double _DelayValue;
            /// <summary>
            /// Delay值
            /// </summary>
            public double DelayValue
            {
                get { return _DelayValue; }
                set { SetProperty(ref _DelayValue, value); }
            }
        }
        #endregion

        #region 方法
        private readonly FlowModel flowModel = new();
        /// <summary>
        /// 运行
        /// </summary>
        public void Run()
        {
            if (AxisX == null || AxisY == null) return;
            switch (flowModel.Step)
            {
                case 1:

                    AxisX.Interpolations = [.. Interpolations];
                    AxisX.InterpolationGroupParams = Params;
                    AxisX.MotionFlow.Step = 31;
                    flowModel.Step++;
                    break;
                case 10:
                    if (AxisX.InterpolationDone)
                    {
                        flowModel.Step = 0;
                        flowModel.Done = true;
                    }

                    break;
            }

        }

        /// <summary>
        /// Go
        /// </summary>
        /// <returns>返回布尔值</returns>
        public bool Go()
        {
            flowModel.Done = false;

            if (AxisX == null || AxisY == null)
            {
                flowModel.Step = 0;
                return false;
            }
            if (Interpolations.Count < 1)
            {
                flowModel.Step = 0;
                return false;
            }
            flowModel.Step = 1;
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
            AxisX?.Stop();
            AxisY?.Stop();
            flowModel.Reset();

        }


        #endregion
    }


}
