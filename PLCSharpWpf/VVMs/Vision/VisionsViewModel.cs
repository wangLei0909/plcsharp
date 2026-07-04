using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.VVMs.Vision.Camera;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Windows.Media.Imaging;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// Visions视图模型
    /// </summary>
    public class VisionsViewModel : DialogAwareBase
    {


        /// <summary>
        /// Visions视图模型
        /// </summary>
        public VisionsViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {

            VisionsModel = container.Resolve<VisionsModel>();

        }
        /// <summary>
        /// 视觉模型
        /// </summary>
        public VisionsModel VisionsModel { get; }

        private Mat _ShowMat;
        /// <summary>
        /// 显示矩阵
        /// </summary>
        public Mat ShowMat
        {
            get { return _ShowMat; }
            set
            {
                SetProperty(ref _ShowMat, value);
                if (value == null) return;
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (_ImgSrc != null
                    && _ShowMat.Width == _ImgSrc.PixelWidth
                    && _ShowMat.Height == _ImgSrc.PixelHeight)
                    {
                        WriteableBitmapConverter.ToWriteableBitmap(_ShowMat, _ImgSrc);

                    }
                    else
                    {
                        ImgSrc = _ShowMat.ToWriteableBitmap();
                    }
                }));


            }
        }
        private WriteableBitmap _ImgSrc;

        /// <summary>
        /// 显示的图像源
        /// </summary>
        public WriteableBitmap ImgSrc
        {
            get { return _ImgSrc; }
            set { SetProperty(ref _ImgSrc, value); }
        }

        private CameraBase _Camera;
        /// <summary>
        /// 配置项
        /// </summary>
        public CameraBase Camera
        {
            get { return _Camera; }
            set
            {
                if (cameraWorking) return;

                SetProperty(ref _Camera, value);
            }
        }

        private DelegateCommand _SearchCameras;
        /// <summary>
        /// SearchCameras
        /// </summary>
        public DelegateCommand SearchCameras =>
            _SearchCameras ??= new DelegateCommand(ExecuteSearchCameras);

        void ExecuteSearchCameras()
        {
            VisionsModel.SearchCameras();
        }

        private DelegateCommand _AddCameras;
        /// <summary>
        /// 添加Cameras
        /// </summary>
        public DelegateCommand AddCameras =>
            _AddCameras ??= new DelegateCommand(ExecuteAddCameras);

        void ExecuteAddCameras()
        {
            if (Camera == null) return;
            var camera = VisionsModel.Cameras.Where(c => c.Name == Camera.Name).FirstOrDefault();
            if (camera == null)
            {
                VisionsModel.Cameras.Add(Camera);

            }
            else
            {
                SendInfoDialog("无法添加同名相机");
            }


        }
        private DelegateCommand _StopContinuous;
        /// <summary>
        /// 停止Continuous
        /// </summary>
        public DelegateCommand StopContinuous =>
            _StopContinuous ??= new DelegateCommand(ExecuteStopContinuous);

        void ExecuteStopContinuous()
        {
            Camera.StopContinuous();
            continuous = false;

        }
        bool continuous;
        private AsyncDelegateCommand<object> _CameraCommand;
        /// <summary>
        /// 相机Command
        /// </summary>
        public AsyncDelegateCommand<object> CameraCommand =>
            _CameraCommand ??= new AsyncDelegateCommand<object>(ExecuteCameraCommandAsync);

        private async Task ExecuteCameraCommandAsync(object param)
        {
            if (Camera is null) return;
            var cmd = param as string;


            if (cameraWorking) return;
            cameraWorking = true;
            await Task.Run(() =>
            {

                FlowModel flow = new();
                switch (cmd)
                {
                    case "Trig":
                        {
                            while (flow.Done == false)
                            {

                                switch (flow.Step)
                                {
                                    case 0:
                                        Camera.SoftwareTrig();

                                        flow.Step++;
                                        break;
                                    case 1:
                                        if (Camera.WaitOne(3000))
                                        {
                                            ShowMat = Camera.Mat;
                                            flow.Step++;
                                        }

                                        break;
                                    case 2:
                                        Camera.SetTriggerSource();

                                        flow.Done = true;
                                        break;

                                }
                                if (flow.CheckFlowTime(10))
                                {

                                    break;
                                }
                            }

                        }
                        break;
                    case "Continuous":
                        {
                            while (flow.Done == false)
                            {

                                switch (flow.Step)
                                {
                                    case 0:
                                        Camera.Continuous();
                                        continuous = true;
                                        flow.Step++;
                                        break;
                                    case 1:
                                        if (Camera.WaitOne(3000))
                                        {
                                            ShowMat = Camera.Mat;

                                        }
                                        if (continuous == false)
                                        {

                                            flow.Step++;
                                        }
                                        break;
                                    case 2:
                                        Camera.SetTriggerSource();

                                        flow.Done = true;
                                        break;

                                }

                            }

                        }
                        break;
                }


            });
            cameraWorking = false;
        }
        bool cameraWorking;
    }
}