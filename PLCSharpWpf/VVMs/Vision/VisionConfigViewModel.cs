using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PLCSharp.Core.Prism;
using PLCSharp.Core.UserControls;
using PLCSharp.Models;
using PLCSharp.VVMs.GlobalVariables;
using PLCSharp.VVMs.Vision.Camera;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// 视觉流程配置界面的视图模型，管理流程步骤的增删、参数编辑和功能测试
    /// </summary>
    public partial class VisionConfigViewModel : DialogAwareBase
    {
        #region 基础设施
        private readonly IDialogService _dialogService;

        /// <summary>
        /// Vision配置视图模型
        /// </summary>
        public VisionConfigViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            _dialogService = dialogService;
            GlobalModel = container.Resolve<GlobalModel>();
            VisionsModel = GlobalModel.VisionsModel;
            SystemVariables = GlobalModel.VariablesModel.SystemVariables;

            FlowMenuGroups = BuildFlowMenuGroups();
            ImageEdit = new ImageEdit();
            Binding binding = new("ImgSrc");
            ImageEdit.DataContext = this;
            ImageEdit.SetBinding(ImageEdit.ImageSourceProperty, binding);
        }
        /// <summary>全局模型</summary>
        public GlobalModel GlobalModel { get; set; }
        private ImageEdit _ImageEdit;
        /// <summary>
        /// 图像编辑控件，用于显示和编辑图像
        /// </summary>
        public ImageEdit ImageEdit
        {
            get { return _ImageEdit; }
            set { SetProperty(ref _ImageEdit, value); }
        }

        /// <summary>
        /// 视觉模型
        /// </summary>
        public VisionsModel VisionsModel { get; set; }

        private VisionFunction _SelectedVisionFunction;
        /// <summary>
        /// 选择的视觉功能
        /// </summary>
        public VisionFunction SelectedVisionFunction
        {
            get { return _SelectedVisionFunction; }
            set { SetProperty(ref _SelectedVisionFunction, value); }
        }

        private UserControl _ContentRegion;
        /// <summary>
        /// 内容区域
        /// </summary>
        public UserControl ContentRegion
        {
            get { return _ContentRegion; }
            set { SetProperty(ref _ContentRegion, value); }
        }

        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            SelectedVisionFunction = parameters.GetValue<VisionFunction>("SelectedVisionFunction");
            SelectedVisionFunction.EditImageEdit = ImageEdit;
        }

        /// <summary>
        /// 关闭对话框后要执行的
        /// </summary>
        public override void OnDialogClosed()
        {
            SelectedVisionFunction.EditImageEdit = null;
        }

 
        /// <summary>
        /// Line 类型局部变量列表（供 ComboBox 选择）
        /// </summary>
        public IEnumerable<LocalVariableItem> LineVariables =>
            SelectedVisionFunction?.Params?.Variables?.Where(v => v.VarType == "Line") ?? [];

        public IEnumerable<LocalVariableItem> AllVariables =>
            SelectedVisionFunction?.Params?.Variables ?? [];



        #endregion

        #region 全局图像管理
        private ImageData _GridSelectedGlobalImageData;
        /// <summary>
        /// 选择图像数据
        /// </summary>
        public ImageData GridSelectedGlobalImageData
        {
            get { return _GridSelectedGlobalImageData; }
            set { SetProperty(ref _GridSelectedGlobalImageData, value); }
        }

        private DelegateCommand<string> _GlobalImageManage;
        /// <summary>
        /// 图像管理
        /// </summary>
        public DelegateCommand<string> GlobalImageManage =>
            _GlobalImageManage ??= new DelegateCommand<string>(ExecuteImageManage);

        void ExecuteImageManage(string cmd)
        {
            switch (cmd)
            {
                case "New":
                    var newImage = new ImageData()
                    {
                    };
                    VisionsModel.ImageDatas.Add(newImage);

                    break;
                case "Remove":
                    if (GridSelectedGlobalImageData != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除图像 [{GridSelectedGlobalImageData.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        var image = VisionsModel._DatasContext.ImageDatas.Where(c => c.ID == GridSelectedGlobalImageData.ID).FirstOrDefault();
                        if (image != null)
                        {

                            VisionsModel._DatasContext.ImageDatas.Remove(image);
                            VisionsModel._DatasContext.Save();
                        }
                        var name = GridSelectedGlobalImageData.Name;
                        VisionsModel.ImageDatas.Remove(GridSelectedGlobalImageData);
                        SendInfoDialog($"已删除：{name}");
                    }
                    break;
                case "Save":
                    var names = new List<string>();

                    foreach (var item in VisionsModel.ImageDatas)
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


                    foreach (var item in VisionsModel.ImageDatas)
                    {
                        if (VisionsModel._DatasContext.ImageDatas.Any(h => h.ID == item.ID) == false)
                        {
                            item.RecipeID = VisionsModel._DatasContext.CurrentRecipe.ID;
                            VisionsModel._DatasContext.ImageDatas.Add(item);

                        }
                        else
                        {
                            var newitem = VisionsModel._DatasContext.ImageDatas.Where(c => c.Name == item.Name).FirstOrDefault();
                            newitem.ID = item.ID;
                            newitem.RecipeID = item.RecipeID;
                            newitem.Name = item.Name;
                            newitem.Comment = item.Comment;
                            newitem.Mat = item.Mat;


                        }


                    }
                    if (GridSelectedGlobalImageData != null)
                        GridSelectedGlobalImageData.Prompt = "";
                    VisionsModel._DatasContext.Save();
                    break;
                case "Show":
                    if (GridSelectedGlobalImageData.Mat != null && GridSelectedGlobalImageData.Mat.Empty() == false)
                        ShowMat = GridSelectedGlobalImageData.Mat;
                    break;
            }
        }
        #endregion

        #region 局部图像管理

        private DelegateCommand<string> _LocalImageDataManage;
        /// <summary>
        /// 局部图像管理
        /// </summary>
        public DelegateCommand<string> LocalImageDataManage =>
            _LocalImageDataManage ??= new DelegateCommand<string>(ExecuteMatManage);

        void ExecuteMatManage(string cmd)
        {
            if (SelectedVisionFunction == null) return;
            switch (cmd)
            {
                case "New":
                    var baseName = "temp";
                    int index = 1;
                    while (SelectedVisionFunction.Params.ImageDatas.Where(w => w.Name == $"{baseName}{index}").Any())
                        index++;
                    SelectedVisionFunction.Params.ImageDatas.Add(new ImageData { Name = $"{baseName}{index}" });
                    break;
                case "Remove":
                    if (GridSelectedLocalImageData == null) return;
                    if (SelectedVisionFunction.Params.ImageDatas.Contains(GridSelectedLocalImageData))
                        SelectedVisionFunction.Params.ImageDatas.Remove(GridSelectedLocalImageData);
                    break;
                case "Show":
                    if (GridSelectedLocalImageData.Mat != null && GridSelectedLocalImageData.Mat.Empty() == false)
                        ShowMat = GridSelectedLocalImageData.Mat;
                    break;

            }
        }


        private ImageData _GridSelectedLocalImageData;
        /// <summary>
        /// 在表格中选择的局部图像
        /// </summary>
        public ImageData GridSelectedLocalImageData
        {
            get { return _GridSelectedLocalImageData; }
            set { SetProperty(ref _GridSelectedLocalImageData, value); }
        }


        #endregion

        #region 显示

        private WriteableBitmap _ImgSrc;

        /// <summary>
        /// 显示的图像源
        /// </summary>
        public WriteableBitmap ImgSrc
        {
            get { return _ImgSrc; }
            set { SetProperty(ref _ImgSrc, value); }
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
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                 {
                     try
                     {
                         if (_ImgSrc != null
                          && _ShowMat.Width == _ImgSrc.PixelWidth
                          && _ShowMat.Height == _ImgSrc.PixelHeight
                          && _ShowMat.Channels() == _ImgSrc.Format.BitsPerPixel / 8
                        )
                         {
                             WriteableBitmapConverter.ToWriteableBitmap(_ShowMat, _ImgSrc);

                         }
                         else
                         {
                             ImgSrc = WriteableBitmapConverter.ToWriteableBitmap(_ShowMat);
                         }
                     }
                     catch (Exception ex)
                     {
                         SendErr($"图像转换失败: {ex.Message}");

                     }

                 }));

            }
        }
        #endregion

      
    }
}