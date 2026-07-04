#nullable enable
using Newtonsoft.Json;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.VVMs.Vision.Camera;
using PLCSharp.VVMs.Vision.VisionFlowHandler;
using Prism.Mvvm;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// <summary>
    /// 视觉流程中的一个步骤，包含步骤类型和各类参数集合（字符串、浮点、整数、布尔等）
    /// </summary>
    public class VisionFlow : BindableBase
    {
        private static ObservableDictionary<TKey, TValue> AsObservable<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? source)
            where TKey : notnull
        {
            if (source is ObservableDictionary<TKey, TValue> od) return od;
            return source != null ? new ObservableDictionary<TKey, TValue>(source) : [];
        }

        private VisionFlowType _Type;
        /// <summary>
        /// 流程步骤的功能类型（拍照、阈值、卡尺寻边等）
        /// </summary>
        public VisionFlowType Type
        {
            get { return _Type; }
            set
            {
                SetProperty(ref _Type, value);
            }
        }


        private string _Comment = string.Empty;
        /// <summary>
        /// 步骤备注说明
        /// </summary>
        public string Comment
        {
            get { return _Comment; }
            set { SetProperty(ref _Comment, value); }
        }

        private ObservableDictionary<string, string> _StringParams = [];
        /// <summary>
        /// 字符串类型参数（路径、名称等）
        /// </summary>
        public ObservableDictionary<string, string> StringParams
        {
            get { return _StringParams; }
            set { SetProperty(ref _StringParams, AsObservable(value)); }
        }
 
        private ObservableDictionary<string, double> _DoubleParams = [];
        /// <summary>
        /// 双精度浮点参数（坐标、尺寸等）
        /// </summary>
        public ObservableDictionary<string, double> DoubleParams
        {
            get { return _DoubleParams; }
            set { SetProperty(ref _DoubleParams, AsObservable(value)); }
        }
 
        private ObservableDictionary<string, long> _LongParams = [];
        /// <summary>
        /// 长整数类型参数
        /// </summary>
        public ObservableDictionary<string, long> LongParams
        {
            get { return _LongParams; }
            set { SetProperty(ref _LongParams, AsObservable(value)); }
        }


        private ObservableDictionary<string, int> _IntParams = [];
        /// <summary>
        /// 整数类型参数（阈值、通道索引等）
        /// </summary>
        public ObservableDictionary<string, int> IntParams
        {
            get { return _IntParams; }
            set { SetProperty(ref _IntParams, AsObservable(value)); }
        }
        private ObservableDictionary<string, bool> _BoolParams = [];
        /// <summary>
        /// 布尔类型参数（选项开关）
        /// </summary>
        public ObservableDictionary<string, bool> BoolParams
        {
            get { return _BoolParams; }
            set { SetProperty(ref _BoolParams, AsObservable(value)); }
        }



        private FlowModel _Flow = new();
        /// <summary>
        /// 流程状态模型
        /// </summary>
        [JsonIgnore]
        public FlowModel Flow
        {
            get { return _Flow; }
            set
            {
                if (value != null) _Flow = value;


            }
        }
        /// <summary>
        /// 相机
        /// </summary>
        [JsonIgnore]
        public CameraBase? Camera { get; set; }
    }
}
