using MvCameraControl;
using PLCSharp.Core.Prism;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;

namespace PLCSharp.VVMs.Vision.Camera
{
    /// <summary>
    /// HikCameras
    /// </summary>
    [Model]
    public class HikCameras : ModelBase
    {
        /// <summary>
        /// HikCameras
        /// </summary>
        public HikCameras(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            try
            {

                SDKSystem.Initialize();
            }
            catch
            {
                SendInfoDialog(@"运行本软件需要安装 MVS V4.5 及以上版本运行时，请确保C:\Program Files(x86)\Common Files\MVS\Runtime\Win64_x64 目录存在并包含正确版本的运行时文件");
            }

        }
        /// <summary>
        /// OnExit
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        protected override void OnExit(object sender, EventArgs e)
        {
            SDKSystem.Finalize();
        }

        readonly DeviceTLayerType enumTLayerType = DeviceTLayerType.MvGigEDevice 
         | DeviceTLayerType.MvUsbDevice;
        //| DeviceTLayerType.MvGenTLGigEDevice
        //| DeviceTLayerType.MvGenTLCXPDevice
        //| DeviceTLayerType.MvGenTLCameraLinkDevice
        //| DeviceTLayerType.MvGenTLXoFDevice;


        /// <summary>
        /// Cameras
        /// </summary>
        public ObservableCollection<CameraBase> Cameras { get; set; } = [];

        /// ch:枚举 GIGE 设备 | en:Enum GIGE device
        public void SearchCameras()
        {
            try
            {
                Cameras.Clear();
                int nRet = DeviceEnumerator.EnumDevices(enumTLayerType, out List<IDeviceInfo> deviceInfoList);
                if (nRet != MvError.MV_OK)
                {
                    SendInfoDialog("Enumerate devices fail!");
                    return;
                }

                // ch:在窗体列表中显示设备名 | en:Display device name in the form list
                for (int i = 0; i < deviceInfoList.Count; i++)
                {
                    IDeviceInfo deviceInfo = deviceInfoList[i];
                    if (deviceInfo.ManufacturerName == "Hikrobot" || deviceInfo.ManufacturerName == "Hikvision")
                    {
                        var camera = new HikCamera() { DeviceInfo = deviceInfo };

                        if (deviceInfo.UserDefinedName != "")
                        {
                            camera.Name = deviceInfo.UserDefinedName;
                        }
                        else
                        {
                            camera.Name = deviceInfo.SerialNumber;
                        }
                        camera.Brand = CameraBrand.HikRobot;
                        Cameras.Add(camera);
                    }
                }


            }
            catch (Exception ex)
            {
                SendInfoDialog(ex.Message);
                return;
            }
        }


        /************************************************************************
         *  @fn     IsColorData()
         *  @brief  判断是否是彩色数据
         *  @param  enGvspPixelType         [IN]           像素格式
         *  @return 成功，返回0；错误，返回-1
         ************************************************************************/
        /// <summary>
        /// IsMonoPixelFormat
        /// </summary>
        /// <param name="enType">en类型</param>
        /// <returns>返回布尔值</returns>
        public static bool IsMonoPixelFormat(MvGvspPixelType enType)
        {
            switch (enType)
            {
                case MvGvspPixelType.PixelType_Gvsp_Mono8:
                case MvGvspPixelType.PixelType_Gvsp_Mono10:
                case MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case MvGvspPixelType.PixelType_Gvsp_Mono12:
                case MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// IsColorPixelFormat
        /// </summary>
        /// <param name="enType">en类型</param>
        /// <returns>返回布尔值</returns>
        public static bool IsColorPixelFormat(MvGvspPixelType enType)
        {
            switch (enType)
            {
                case MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                case MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case MvGvspPixelType.PixelType_Gvsp_YUV422_Packed:
                case MvGvspPixelType.PixelType_Gvsp_YUV422_YUYV_Packed:
                case MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                case MvGvspPixelType.PixelType_Gvsp_BayerGB10:
                case MvGvspPixelType.PixelType_Gvsp_BayerGB10_Packed:
                case MvGvspPixelType.PixelType_Gvsp_BayerBG10:
                case MvGvspPixelType.PixelType_Gvsp_BayerBG10_Packed:
                case MvGvspPixelType.PixelType_Gvsp_BayerRG10:
                case MvGvspPixelType.PixelType_Gvsp_BayerRG10_Packed:
                case MvGvspPixelType.PixelType_Gvsp_BayerGR10:
                case MvGvspPixelType.PixelType_Gvsp_BayerGR10_Packed:
                case MvGvspPixelType.PixelType_Gvsp_BayerGB12:
                case MvGvspPixelType.PixelType_Gvsp_BayerGB12_Packed:
                case MvGvspPixelType.PixelType_Gvsp_BayerBG12:
                case MvGvspPixelType.PixelType_Gvsp_BayerBG12_Packed:
                case MvGvspPixelType.PixelType_Gvsp_BayerRG12:
                case MvGvspPixelType.PixelType_Gvsp_BayerRG12_Packed:
                case MvGvspPixelType.PixelType_Gvsp_BayerGR12:
                case MvGvspPixelType.PixelType_Gvsp_BayerGR12_Packed:
                    return true;
                default:
                    return false;
            }
        }


    }
}