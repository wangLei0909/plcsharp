using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Threading;
using System.Windows.Media.Imaging;

namespace PLCSharp.VVMs.Vision.Camera
{
    /// <summary>
    /// 相机配置视图模型
    /// </summary>
    public class CameraConfigViewModel : DialogAwareBase
    {
        /// <summary>
        /// 相机配置视图模型
        /// </summary>
        public CameraConfigViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {

        }

        private CameraBase _Camera;
        /// <summary>
        /// 配置项
        /// </summary>
        public CameraBase Camera
        {
            get { return _Camera; }
            set { SetProperty(ref _Camera, value); }
        }


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
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(new Action(() =>
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
        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            var camera = parameters.GetValue<CameraBase>("Camera");
            if (camera == null)
            {

            }
            else
            {

                Title = camera.Name;
                Camera = camera;
            }

        }
        /// <summary>
        /// 关闭对话框后要执行的
        /// </summary>
        public override void OnDialogClosed()
        {
            Camera.StopContinuous();

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
                                Thread.Sleep(1);
                                switch (flow.Step)
                                {
                                    case 0:
                                        Camera.Trig();
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
                                Thread.Sleep(1);
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

