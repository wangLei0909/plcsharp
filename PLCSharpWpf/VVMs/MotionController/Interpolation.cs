using Newtonsoft.Json;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// 插补
    /// </summary>
    public class Interpolation : BindableBase
    {
        #region 属性

        private string _Name;
        /// <summary>
        /// 点名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }

        private InterpolationType _Type;
        /// <summary>
        /// 类型
        /// </summary>
        public InterpolationType Type
        {
            get { return _Type; }
            set { SetProperty(ref _Type, value); }
        }

        /// <summary>
        /// 序列化Interpolations
        /// </summary>
        public string SerializedInterpolations
        {
            get => JsonConvert.SerializeObject(_InterpolationPoints); // 自动序列化
            set
            {
                InterpolationPoints = JsonConvert.DeserializeObject<ObservableCollection<InterpolationPoint>>(value); // 自动反序列化
            }
        }


        private ObservableCollection<InterpolationPoint> _InterpolationPoints = [];
        /// <summary>
        /// 插补Points
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<InterpolationPoint> InterpolationPoints
        {
            get { return _InterpolationPoints; }
            set { SetProperty(ref _InterpolationPoints, value); }
        }



        private InterpolationParams _Params;

        /// <summary>
        /// 配置项
        /// </summary>
        public InterpolationParams Params
        {
            get
            {
                _Params ??= new InterpolationParams();

                return _Params;
            }
            set
            {
                SetProperty(ref _Params, value);
            }
        }
        /// <summary>
        /// 插补Params
        /// </summary>
        public class InterpolationParams : BindableBase
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

            private ushort _Dir;
            /// <summary>
            /// Dir
            /// </summary>
            public ushort Dir
            {
                get { return _Dir; }
                set { SetProperty(ref _Dir, value); }
            }

            private ushort _PositionMode;
            /// <summary>
            /// 位置Mode
            /// </summary>
            public ushort PositionMode
            {
                get { return _PositionMode; }
                set { SetProperty(ref _PositionMode, value); }
            }
        }
        #endregion

    }

    public enum InterpolationType
    {
        Line直线 = 0,
        Arc圆弧 = 1
    }
}
