using OpenCvSharp.WpfExtensions;
using PLCSharp.Core.Prism;
using PLCSharp.Core.Tools;
using PLCSharp.Core.UserControls;
using PLCSharp.Models;
using PLCSharp.VVMs.Connects;
using PLCSharp.VVMs.Vision.Camera;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.Windows;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// 视觉模型
    /// </summary>
    [Model]
    public class VisionsModel : ModelBase
    {
        /// <summary>
        /// 视觉模型
        /// </summary>
        public VisionsModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {

            _HikCameras = container.Resolve<HikCameras>();
            _HikCameras.SearchCameras();
            foreach (var item in _DatasContext.Cameras)
            {

                switch (item.Brand)
                {
                    case CameraBrand.HikRobot:
                        var camera = _HikCameras.Cameras.Where(c => c.Name == item.Name).FirstOrDefault();
                        if (camera != null)
                        {
                            camera.Params = item.Params;
                            camera.ID = item.ID;
                            camera.Comment = item.Comment;
                            camera.Prompt = "";
                            Cameras.Add(camera);
                        }
                        else
                        {
                            Cameras.Add(item);
                            System.Windows.MessageBox.Show($"未发现相机{item.Name}，请检查连接");
                        }
                        break;

                }

            }
        }

        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="globalModel">全局模型</param>
        public void Init(GlobalModel globalModel)
        {
            GlobalModel = globalModel;
            Connects = GlobalModel.Connects;
        }

        /// <summary>
        /// SearchCameras
        /// </summary>
        internal void SearchCameras()
        {
            CamerasAll.Clear();
            _HikCameras.SearchCameras();
            foreach (var item in _HikCameras.Cameras)
            {
                CamerasAll.Add(item);
            }
        }
        /// <summary>
        /// 连接模型
        /// </summary>
        public ConnectsModel Connects { get; set; }

        private ObservableCollection<VisionFunction> _VisionFunctions = [];
        /// <summary>
        /// VisionFunctions
        /// </summary>
        public ObservableCollection<VisionFunction> VisionFunctions
        {
            get { return _VisionFunctions; }
            set { SetProperty(ref _VisionFunctions, value); }
        }

        private VisionFunction _SelectedVisionFunction;
        /// <summary>
        /// SelectedVisionFunction
        /// </summary>
        public VisionFunction SelectedVisionFunction
        {
            get { return _SelectedVisionFunction; }
            set { SetProperty(ref _SelectedVisionFunction, value); }
        }

        private DelegateCommand<string> _Manage;
        /// <summary>
        /// 管理
        /// </summary>
        public DelegateCommand<string> Manage =>
            _Manage ??= new DelegateCommand<string>(ExecuteManage);

        void ExecuteManage(string cmd)
        {
            switch (cmd)
            {
                case "New":
                    var newFunction = new VisionFunction()
                    {
                        VisionsModel = this,
                        ImageDatas = ImageDatas,
                    };
                    VisionFunctions.Add(newFunction);

                    break;
                case "Remove":
                    if (SelectedVisionFunction != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除视觉功能 [{SelectedVisionFunction.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        var visionFunction = _DatasContext.VisionFunctions.Where(c => c.ID == SelectedVisionFunction.ID).FirstOrDefault();
                        if (visionFunction != null)
                        {
                            _DatasContext.VisionFunctions.Remove(visionFunction);
                            _DatasContext.Save();
                        }
                        var name = SelectedVisionFunction.Name;
                        VisionFunctions.Remove(SelectedVisionFunction);
                        SendInfoDialog($"已删除：{name}");

                    }
                    break;
                case "Save":
                    var names = new List<string>();

                    foreach (var item in VisionFunctions)
                    {
                        if (string.IsNullOrEmpty(item.Name))
                        {
                            SendInfoDialog($"保存失败，名称{item.Name}不合适！");
                            return;
                        }

                        if (names.Contains(item.Name))
                        {
                            SendInfoDialog($"保存失败，重复的名称{item.Name}！");
                            return;
                        }
                        else
                        {
                            names.Add(item.Name);
                        }
                    }


                    foreach (var item in VisionFunctions)
                    {
                        if (_DatasContext.VisionFunctions.Any(h => h.ID == item.ID) == false)
                        {
                            item.RecipeID = _DatasContext.CurrentRecipe.ID;
                            var serializedVisionFlows = item.SerializedVisionFlows;
                            _DatasContext.VisionFunctions.Add(item);

                        }
                        else
                        {
                            var newitem = _DatasContext.VisionFunctions.Where(c => c.ID == item.ID).FirstOrDefault();

                            newitem.RecipeID = item.RecipeID;
                            newitem.Name = item.Name;
                            newitem.Comment = item.Comment;
                            newitem.SerializedVisionFlows = item.SerializedVisionFlows;
                            newitem.SerializedVisionParams = item.SerializedVisionParams;
                            newitem.ControlName = item.ControlName;

                        }


                    }
                    _DatasContext.Save();
                    SelectedVisionFunction.Prompt = "";

                    break;

                case "Config":

                    if (SelectedVisionFunction == null) return;
                    var dialogParams = new DialogParameters
                        {
                            { "SelectedVisionFunction", SelectedVisionFunction}
                        };

                    _dialogService.Show("VisionConfig", dialogParams, r =>
                {


                });

                    break;
            }
        }

        #region Camera
        private HikCameras _HikCameras;
        private ObservableCollection<CameraBase> _Cameras = [];
        /// <summary>
        /// Cameras
        /// </summary>
        public ObservableCollection<CameraBase> Cameras
        {
            get { return _Cameras; }
            set { SetProperty(ref _Cameras, value); }
        }

        private ObservableCollection<CameraBase> _CamerasAll = [];
        /// <summary>
        /// CamerasAll
        /// </summary>
        public ObservableCollection<CameraBase> CamerasAll
        {
            get { return _CamerasAll; }
            set { SetProperty(ref _CamerasAll, value); }
        }
        private CameraBase _SelectedCamera;
        /// <summary>
        /// 选中的相机
        /// </summary>
        public CameraBase SelectedCamera
        {
            get { return _SelectedCamera; }
            set { SetProperty(ref _SelectedCamera, value); }
        }


        private DelegateCommand<string> _CamreraManage;
        /// <summary>
        /// Camrera管理
        /// </summary>
        public DelegateCommand<string> CamreraManage =>
            _CamreraManage ??= new DelegateCommand<string>(ExecuteCamreraManage);

        void ExecuteCamreraManage(string cmd)
        {
            switch (cmd)
            {
                case "New":

                    Cameras.Add(new CameraBase());

                    break;


                case "Save":
                    SaveCameras();
                    break;

                case "Config":
                    if (SelectedCamera != null)
                    {
                        var dialogParams = new DialogParameters
                        {
                            { "Camera", SelectedCamera }
                        };

                        _dialogService.Show("CameraConfig", dialogParams, r =>
                        {


                        });
                    }
                    break;

                case "Remove":
                    if (SelectedCamera != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除相机 [{SelectedCamera.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        var camera = _DatasContext.Cameras.Where(c => c.ID == SelectedCamera.ID).FirstOrDefault();
                        if (camera != null)
                        {
                            _DatasContext.Cameras.Remove(camera);
                            _DatasContext.Save();
                            var name = camera.Name;
                            Cameras.Remove(SelectedCamera);
                            SendInfoDialog($"已删除相机：{name}");
                        }
                    }
                    break;

            }

        }

        private void SaveCameras()
        {
            var names = new List<string>();

            foreach (var item in Cameras)
            {
                if (string.IsNullOrEmpty(item.Name))
                {
                    SendInfoDialog($"保存失败，名称{item.Name}不合适！");
                    return;
                }

                if (names.Contains(item.Name))
                {
                    SendInfoDialog($"保存失败，重复的名称{item.Name}！");
                    return;
                }
                else
                {
                    names.Add(item.Name);
                }
            }


            foreach (var item in Cameras)
            {
                if (!_DatasContext.Cameras.Any(h => h.ID == item.ID))
                {
                    _DatasContext.Cameras.Add(item);

                }
                else
                {
                    var camera = _DatasContext.Cameras.Where(c => c.Name == item.Name).FirstOrDefault();

                    camera.Params = item.Params;
                    camera.Comment = item.Comment;

                }


            }
            _DatasContext.Save();
            SelectedCamera.Prompt = "";

        }

        #endregion Camera

        #region 图像列表
        private ObservableCollection<ImageData> _ImageDatas = [];
        /// <summary>
        /// 全局图像列表
        /// </summary>
        public ObservableCollection<ImageData> ImageDatas
        {
            get { return _ImageDatas; }
            set { SetProperty(ref _ImageDatas, value); }
        }

        private ImageData _SelectImageData;
        /// <summary>
        /// 选择图像数据
        /// </summary>
        public ImageData SelectImageData
        {
            get { return _SelectImageData; }
            set { SetProperty(ref _SelectImageData, value); }
        }

        private DelegateCommand<string> _ImageManage;
        /// <summary>
        /// 图像管理
        /// </summary>
        public DelegateCommand<string> ImageManage =>
            _ImageManage ??= new DelegateCommand<string>(ExecuteImageManage);

        void ExecuteImageManage(string cmd)
        {
            switch (cmd)
            {
                case "New":
                    var newImage = new ImageData()
                    {
                    };
                    ImageDatas.Add(newImage);

                    break;
                case "Remove":
                    if (SelectImageData != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除图像 [{SelectImageData.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        var image = _DatasContext.ImageDatas.Where(c => c.ID == SelectImageData.ID).FirstOrDefault();
                        if (image != null)
                        {

                            _DatasContext.ImageDatas.Remove(image);
                            _DatasContext.Save();
                        }
                        var name = SelectImageData.Name;
                        ImageDatas.Remove(SelectImageData);
                        SendInfoDialog($"已删除：{name}");
                    }
                    break;
                case "Save":
                    var names = new List<string>();

                    foreach (var item in ImageDatas)
                    {
                        if (string.IsNullOrEmpty(item.Name))
                        {
                            SendInfoDialog($"保存失败，名称{item.Name}不合适！"    );
                            return;
                        }

                        if (names.Contains(item.Name))
                        {
                            SendInfoDialog($"保存失败，重复的名称{item.Name}！");
                            return;
                        }
                        else
                        {
                            names.Add(item.Name);
                        }
                    }


                    foreach (var item in ImageDatas)
                    {
                        if (_DatasContext.ImageDatas.Any(h => h.ID == item.ID) == false)
                        {
                            item.RecipeID = _DatasContext.CurrentRecipe.ID;
                            _DatasContext.ImageDatas.Add(item);

                        }
                        else
                        {
                            var newitem = _DatasContext.ImageDatas.Where(c => c.Name == item.Name).FirstOrDefault();
                            newitem.ID = item.ID;
                            newitem.RecipeID = item.RecipeID;
                            newitem.Name = item.Name;
                            newitem.Comment = item.Comment;
                            newitem.Mat = item.Mat;


                        }


                    }
                    if (SelectImageData != null)
                        SelectImageData.Prompt = "";
                    _DatasContext.Save();
                    break;
                case "Show":

                    var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                                .FirstOrDefault(w => w.IsActive);
                    var imageEdit = WpfTool.FindVisualChild<ImageEdit>(window);


                    imageEdit.ImageSource = SelectImageData.Mat.ToWriteableBitmap();
                    break;
            }
        }



        #endregion
    }
}