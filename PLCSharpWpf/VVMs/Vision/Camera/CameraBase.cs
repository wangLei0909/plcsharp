using Newtonsoft.Json;
using OpenCvSharp;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;

namespace PLCSharp.VVMs.Vision.Camera
{
    /// <summary>
    /// 相机Base
    /// </summary>
    public partial class CameraBase : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
        private string _Name;
        /// <summary>
        /// 相机配置
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

        private CameraBrand _Brand;
        /// <summary>
        /// 相机配置
        /// </summary>
        public CameraBrand Brand
        {
            get { return _Brand; }
            set
            {
                SetProperty(ref _Brand, value);
                Prompt = "已修改，请保存";
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
            get => JsonConvert.SerializeObject(Params); // 自动序列化
            set => Params = JsonConvert.DeserializeObject<CameraParams>(value); // 自动反序列化

        }
        //-------------------------------------------------------NotMapped--------------------------------------------

        private bool _LogSwitch;
        /// <summary>
        /// 日志Switch
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public bool LogSwitch
        {
            get { return _LogSwitch; }
            set { SetProperty(ref _LogSwitch, value); }
        }
        /// <summary>
        /// 日志Queue
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public ConcurrentQueue<string> LogQueue { get; set; } = [];

        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="log">日志</param>
        public void Log(string log)
        {
            if (LogSwitch)
            { LogQueue.Enqueue(log); }
        }
        private string _Prompt;
        /// <summary>
        /// 提示
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public string Prompt
        {
            get { return _Prompt; }
            set { SetProperty(ref _Prompt, value); }
        }


        private bool _Connected;
        /// <summary>
        /// Connected
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public bool Connected
        {
            get { return _Connected; }
            set { SetProperty(ref _Connected, value); }
        }

        private CameraParams _Params;
        /// <summary>
        /// 参数集合
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public CameraParams Params
        {
            get
            {
                _Params ??= new CameraParams();

                return _Params;
            }
            set
            {


                SetProperty(ref _Params, value);



            }
        }


        //-------------------------------------------------------Method--------------------------------------------
        private DelegateCommand<string> _Command;
        /// <summary>
        /// Command
        /// </summary>
        public DelegateCommand<string> Command =>
            _Command ??= new DelegateCommand<string>(ExecuteOpenCommand);

        void ExecuteOpenCommand(string cmd)
        {
            switch (cmd)
            {
                case "Open":
                    Open();
                    break;
                case "Trig":
                    SoftwareTrig();
                    break;
                case "Continuous":
                    Continuous();
                    break;
                case "StopContinuous":
                    StopContinuous();
                    break;
            }

        }

        /// <summary>
        /// 打开
        /// </summary>
        /// <returns>返回布尔值</returns>
        public virtual bool Open() { return false; }

        /// <summary>
        /// 设置ExposureTime
        /// </summary>
        /// <returns>返回布尔值</returns>
        public virtual bool SetExposureTime() { return false; }
        /// <summary>
        /// 设置TriggerSource
        /// </summary>
        /// <returns>返回布尔值</returns>
        public virtual bool SetTriggerSource() { return false; }
        /// <summary>
        /// SoftwareTrig
        /// </summary>
        public virtual void SoftwareTrig() { return; }
        /// <summary>
        /// Trig
        /// </summary>
        public virtual void Trig() { return; }

        /// <summary>
        /// Continuous
        /// </summary>
        public virtual void Continuous() { return; }
        /// <summary>
        /// 停止Continuous
        /// </summary>
        public virtual void StopContinuous() { return; }
        /// <summary>
        /// 关闭
        /// </summary>
        public virtual void Close() { }

        /// <summary>
        /// _imageReadyEvent
        /// </summary>
        protected readonly AutoResetEvent _imageReadyEvent = new(false);
        /// <summary>
        /// WaitOne
        /// </summary>
        /// <param name="timeout">超时时间</param>
        /// <returns>返回布尔值</returns>
        public virtual bool WaitOne(int timeout)
        {

            return false;
        }

        /// <summary>
        /// 图像矩阵
        /// </summary>
        [NotMapped]

        public Mat Mat { get; set; }
        /// <summary>
        /// 相机Params
        /// </summary>
        public class CameraParams : BindableBase
        {


            private double _MinExposureTime;


            /// <summary>
            /// 最小ExposureTime
            /// </summary>
            public double MinExposureTime
            {
                get { return _MinExposureTime; }
                set { SetProperty(ref _MinExposureTime, value); }
            }

            private double _MaxExposureTime;


            /// <summary>
            /// 最大ExposureTime
            /// </summary>
            public double MaxExposureTime
            {
                get { return _MaxExposureTime; }
                set { SetProperty(ref _MaxExposureTime, value); }
            }



            private double _ExposureTime;
            /// <summary>
            /// 相机配置
            /// </summary>
            public double ExposureTime
            {
                get { return _ExposureTime; }
                set
                {
                    SetProperty(ref _ExposureTime, value);

                }
            }



            private TriggerMethod _TriggerSource = TriggerMethod.Software;
            /// <summary>
            /// 相机配置
            /// </summary>
            public TriggerMethod TriggerSource
            {
                get { return _TriggerSource; }
                set
                {
                    SetProperty(ref _TriggerSource, value);

                }
            }

        }
    }
}
