using MvCameraControl;
using OpenCvSharp;
using System.Threading;
using System.Windows;

namespace PLCSharp.VVMs.Vision.Camera
{
    /// <summary>
    /// Hik相机
    /// </summary>
    public class HikCamera : CameraBase
    {
        /// <summary>
        /// 相机
        /// </summary>
        public IDevice Camera { get; set; }

        /// <summary>
        /// Device信息
        /// </summary>
        public IDeviceInfo DeviceInfo { get; set; }

        //private bool continuousAquisition = false;

        bool inited;
        /// <summary>
        /// 设置ExposureTime
        /// </summary>
        /// <returns>返回布尔值</returns>
        public override bool SetExposureTime()
        {
            var nRet = Camera.Parameters.SetFloatValue("ExposureTime", (float)Params.ExposureTime);
            if (nRet != MvError.MV_OK)
            {
                Log(ShowErrorMsg(nRet));
                return false;
            }
            else
            {
                return true;

            }
        }
        /// <summary>
        /// 设置TriggerSource
        /// </summary>
        /// <returns>返回布尔值</returns>
        public override bool SetTriggerSource()
        {
            var nRet = Camera.Parameters.SetEnumValue("TriggerSource", (uint)Params.TriggerSource);

            if (nRet != MvError.MV_OK)
            {
                Log(ShowErrorMsg(nRet));
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// 打开
        /// </summary>
        /// <returns>返回布尔值</returns>
        public override bool Open()
        {
            if (inited) return true;

            if (DeviceInfo == null) return false;
            //ch: 创建设备 | en: Create device
            Camera = DeviceFactory.CreateDevice(DeviceInfo);
            // ch:打开设备 | en:Open device

            int nRet = Camera.Open();

            if (nRet != MvError.MV_OK)
            {
                Log(ShowErrorMsg(nRet));
                return false;
            }
            else
            {
                // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)

                int result;
                if (Camera is IGigEDevice gigEDevice)
                {
                    result = gigEDevice.GetOptimalPacketSize(out int optionPacketSize);
                    if (result != MvError.MV_OK)
                    {
                        ShowErrorMsg(result, "Warning: Get Packet Size failed!");
                    }
                    else
                    {
                        result = Camera.Parameters.SetIntValue("GevSCPSPacketSize", optionPacketSize);
                        if (result != MvError.MV_OK)
                        {
                            ShowErrorMsg(result, "Warning: Set Packet Size failed!");
                        }
                    }
                }

                _ = Camera.Parameters.GetIntValue("OffsetX", out IIntValue OffsetX);
                _ = Camera.Parameters.GetIntValue("OffsetY", out IIntValue OffsetY);
                _ = Camera.Parameters.GetIntValue("Width", out IIntValue Windth);
                _ = Camera.Parameters.GetIntValue("Height", out IIntValue Height);

                int offsetX = (int)OffsetX.CurValue;
                int offsetY = (int)OffsetY.CurValue;

                var width = (int)Windth.Max + offsetX;
                var height = (int)Height.Max + offsetY;

                FullRect = new(0, 0, width, height);
 
                GetMinMaxExposureTime();
                result = Camera.Parameters.SetEnumValueByString("TriggerMode", "On");

                result = Camera.Parameters.SetEnumValue("TriggerSource", (uint)Params.TriggerSource);

                result = Camera.StreamGrabber.StartGrabbing();
                if (result != MvError.MV_OK)
                {

                    receiveThread.Join();
                    Log(ShowErrorMsg(result, "Start Grabbing Fail!"));
                    return false;
                }
                // ch:开始采集 | en:Start Grabbing
                try
                {

                    receiveThread = new Thread(ReceiveThreadProcess)
                    {
                        IsBackground = true
                    };
                    receiveThread.Start();



                }
                catch (Exception ex)
                {
                    MessageBox.Show("Start thread failed!, " + ex.Message);
                    return false;
                }

                inited = true;
                return true;
            }

        }




        IFrameOut frameOut = null;
        Thread receiveThread = null;    // ch:接收图像线程 | en: Receive image thread
        /// <summary>
        /// 接收Thread过程
        /// </summary>
        public void ReceiveThreadProcess()
        {
            while (true)
            {

                int result = Camera.StreamGrabber.GetImageBuffer(1000, out frameOut);
                if (result == MvError.MV_OK)
                {
                    var w = (int)frameOut.Image.Width;
                    var h = (int)frameOut.Image.Height;


                    if (HikCameras.IsMonoPixelFormat(frameOut.Image.PixelType))
                    {
                        Mat = Mat.FromPixelData(h, w, MatType.CV_8U, frameOut.Image.PixelDataPtr);
                    }
                    else if (HikCameras.IsColorPixelFormat(frameOut.Image.PixelType))
                    {
                        Mat = Mat.FromPixelData(h, w, MatType.CV_8UC3, frameOut.Image.PixelDataPtr);
                    }

                    Camera.StreamGrabber.FreeImageBuffer(frameOut);
                    _imageReadyEvent.Set();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        /// <summary>
        /// SoftwareTrig
        /// </summary>
        public override void SoftwareTrig()
        {
            if (Open() == false) return;
            _ = Camera.Parameters.SetEnumValueByString("TriggerMode", "On");
            _ = Camera.Parameters.SetEnumValue("TriggerSource", (uint)TriggerMethod.Software);

            // ch:触发命令 | en:Trigger command
            int result = Camera.Parameters.SetCommandValue("TriggerSoftware");
            if (result != MvError.MV_OK)
            {
                ShowErrorMsg(result, "Trigger Software Fail!");
            }
        }

        /// <summary>
        /// Trig
        /// </summary>
        public override void Trig()
        {
            if (Open() == false) return;
            _ = Camera.Parameters.SetEnumValueByString("TriggerMode", "On");

            _ = Camera.Parameters.SetEnumValue("TriggerSource", (uint)Params.TriggerSource);

            if (Params.TriggerSource == TriggerMethod.Software)
            // ch:触发命令 | en:Trigger command
            {
                int result = Camera.Parameters.SetCommandValue("TriggerSoftware");
                if (result != MvError.MV_OK)
                {
                    ShowErrorMsg(result, "Trigger Software Fail!");
                }
            }
        }
        /// <summary>
        /// Continuous
        /// </summary>
        public override void Continuous()
        {
            if (Open() == false) return;
            // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
            Camera.Parameters.SetEnumValueByString("AcquisitionMode", "Continuous");
            Camera.Parameters.SetEnumValueByString("TriggerMode", "Off");
        }
        /// <summary>
        /// 停止Continuous
        /// </summary>
        public override void StopContinuous()
        {

            if (inited == false) return;

            Camera.Parameters.SetEnumValueByString("TriggerMode", "On");
        }
        /// <summary>
        /// 关闭
        /// </summary>
        public override void Close()
        {

            // ch:停止抓图 | en:Stop grab image
            _ = Camera?.StreamGrabber.StopGrabbing();
            // ch:关闭设备 | en:Close device
            _ = Camera?.Close();
            // ch:销毁设备 | en:Destroy device
            Camera?.Dispose();
        }

        #region

        /// <summary>
        /// 获取最小最大曝光时间
        /// </summary>
        /// <summary>
        /// 获取最小最大ExposureTime
        /// </summary>
        public void GetMinMaxExposureTime()
        {
            if (Camera == null) return;
            Camera.Parameters.GetFloatValue("ExposureTime", out IFloatValue floatValue);
            Params.MinExposureTime = floatValue.Min;
            Params.MaxExposureTime = floatValue.Max;
        }

        #endregion

        #region ROI

        //为了加快拍照速度，这里做了一个取AOI的功能，如果每次都是取固定的AOI,建议使用此功能，如果每次的AOI不同，还是全部取回再截取，因为切换AOI需要时间。
        /// <summary>
        /// 矩形Seting
        /// </summary>
        public OpenCvSharp.Rect RectSeting { get; set; }
        private OpenCvSharp.Rect FullRect;
        private OpenCvSharp.Rect RectLast;
        private OpenCvSharp.Rect ROIRectFomat;

        private void FomatROI()
        {
            _ = Camera.Parameters.GetIntValue("OffsetX", out IIntValue OffsetX);
            _ = Camera.Parameters.GetIntValue("OffsetY", out IIntValue OffsetY);
            _ = Camera.Parameters.GetIntValue("Width", out IIntValue Windth);
            _ = Camera.Parameters.GetIntValue("Height", out IIntValue Height);

            ROIRectFomat.X = RectLast.X - RectLast.X % (int)OffsetX.CurValue;
            ROIRectFomat.Y = RectLast.Y - RectLast.Y % (int)OffsetY.CurValue;
            ROIRectFomat.Width = RectLast.Width - RectLast.Width % (int)Windth.CurValue;
            ROIRectFomat.Height = RectLast.Height - RectLast.Height % (int)Height.CurValue;
            ROIRectFomat.Width = ROIRectFomat.Width < 32 ? 32 : ROIRectFomat.Width;
            ROIRectFomat.Height = ROIRectFomat.Height < 32 ? 32 : ROIRectFomat.Height;

        }
        /// <summary>
        /// 设置ROI
        /// </summary>
        public void SetROI()
        {

            if (RectSeting.Width == 0)
            {
                RectSeting = FullRect;
            }

            if (RectLast != RectSeting)
            {
                RectLast = RectSeting;
                FomatROI();
                _ = Camera.StreamGrabber.StopGrabbing();
                _ = Camera.Parameters.SetIntValue("Width", 32);
                _ = Camera.Parameters.SetIntValue("Height", 32);
                _ = Camera.Parameters.SetIntValue("OffsetX", (uint)ROIRectFomat.Left);
                _ = Camera.Parameters.SetIntValue("OffsetY", (uint)ROIRectFomat.Top);
                _ = Camera.Parameters.SetIntValue("Height", 32);
                _ = Camera.Parameters.SetIntValue("Width", (int)ROIRectFomat.Width);
                _ = Camera.Parameters.SetIntValue("Height", (int)ROIRectFomat.Height);
                _ = Camera.StreamGrabber.StartGrabbing();
            }
        }
        #endregion

        /// <summary>
        /// WaitOne
        /// </summary>
        /// <param name="timeout">超时时间</param>
        /// <returns>返回布尔值</returns>
        public override bool WaitOne(int timeout)
        {
            return _imageReadyEvent.WaitOne(timeout);
        }

        #region SDK
        private static string ShowErrorMsg(int errorCode, string message = "")
        {
            string errorMsg;
            if (errorCode == 0)
            {
                errorMsg = message;
            }
            else
            {
                errorMsg = message + ": Error =" + string.Format("{0:X}", errorCode);
            }

            switch (errorCode)
            {
                case MvError.MV_E_HANDLE:
                    errorMsg += " Error or invalid handle "; break;
                case MvError.MV_E_SUPPORT:
                    errorMsg += " Not supported function "; break;
                case MvError.MV_E_BUFOVER:
                    errorMsg += " Cache is full "; break;
                case MvError.MV_E_CALLORDER:
                    errorMsg += " Function calling order error "; break;
                case MvError.MV_E_PARAMETER:
                    errorMsg += " Incorrect parameter "; break;
                case MvError.MV_E_RESOURCE:
                    errorMsg += " Applying resource failed "; break;
                case MvError.MV_E_NODATA:
                    errorMsg += " No data "; break;
                case MvError.MV_E_PRECONDITION:
                    errorMsg += " Precondition error, or running environment changed "; break;
                case MvError.MV_E_VERSION:
                    errorMsg += " Version mismatches "; break;
                case MvError.MV_E_NOENOUGH_BUF:
                    errorMsg += " Insufficient memory "; break;
                case MvError.MV_E_UNKNOW:
                    errorMsg += " Unknown error "; break;
                case MvError.MV_E_GC_GENERIC:
                    errorMsg += " General error "; break;
                case MvError.MV_E_GC_ACCESS:
                    errorMsg += " Node accessing condition error "; break;
                case MvError.MV_E_ACCESS_DENIED:
                    errorMsg += " No permission "; break;
                case MvError.MV_E_BUSY:
                    errorMsg += " Device is busy, or network disconnected "; break;
                case MvError.MV_E_NETER:
                    errorMsg += " Network error "; break;
            }

            return errorMsg;
        }


        #endregion
    }
}