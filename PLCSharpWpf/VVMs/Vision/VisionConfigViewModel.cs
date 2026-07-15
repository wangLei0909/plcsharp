using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.Core.Tools;
using PLCSharp.Core.UserControls;
using PLCSharp.Models;
using PLCSharp.VVMs.Connects;
using PLCSharp.VVMs.GlobalVariables;
using PLCSharp.VVMs.Vision.Camera;
using PLCSharp.VVMs.Vision.VisionFlowHandler;
using PLCSharp.VVMs.Vision.VisionFlowHandler.Access;
using PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm;
using PLCSharp.VVMs.Vision.VisionFlowHandler.Processing;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// 视觉流程配置界面的视图模型，管理流程步骤的增删、参数编辑和功能测试
    /// </summary>
    public class VisionConfigViewModel : DialogAwareBase
    {
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
            Connects = GlobalModel.Connects;
            FlowMenuGroups = BuildFlowMenuGroups();
            ImageEdit = new ImageEdit();
            Binding binding = new("ImgSrc");
            ImageEdit.DataContext = this;
            ImageEdit.SetBinding(ImageEdit.ImageSourceProperty, binding);
        }

 

        private ImageEdit _ImageEdit;
        /// <summary>
        /// 
        /// </summary>
        public ImageEdit ImageEdit
        {
            get { return _ImageEdit; }
            set { SetProperty(ref _ImageEdit, value); }
        }
        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            SelectedVisionFunction = parameters.GetValue<VisionFunction>("SelectedVisionFunction");
            SelectedVisionFunction.EditImageEdit = ImageEdit;

        }        /// <summary>
                 /// 关闭对话框后要执行的
                 /// </summary>
        public override void OnDialogClosed()
        {
            SelectedVisionFunction.EditImageEdit = null;    
        }

        #region 视觉流程
        private AsyncDelegateCommand _TestFunction;
        /// <summary>
        /// 测试Function
        /// </summary>
        public AsyncDelegateCommand TestFunction =>
            _TestFunction ??= new AsyncDelegateCommand(ExecuteTestFunctionAsync);

        private async Task ExecuteTestFunctionAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var runAllFlow = new FlowModel();
                    while (true)
                    {
                        Thread.Sleep(1);
                        if (SelectedVisionFunction.RunAll(runAllFlow))
                        {
                            return;
                        }
                        else if (runAllFlow.CheckFlowTime(10))
                        {
                            throw new Exception("流程超时!");

                        }
                    }
                });

            }
            catch (Exception ex)
            {

                SendErr(ex.Message);
            }
            if (SelectedVisionFunction.Src == null) return;

            ShowMat = SelectedVisionFunction.Src;

        
             
        }
        private DelegateCommand _Save;
        /// <summary>
        /// 保存
        /// </summary>
        public DelegateCommand Save =>
            _Save ??= new DelegateCommand(ExecuteSave);

        void ExecuteSave()
        {
            VisionsModel.Manage.Execute("Save");
        }


        private AsyncDelegateCommand _TestSelect;
        /// <summary>
        /// 测试选择
        /// </summary>
        public AsyncDelegateCommand TestSelect =>
            _TestSelect ??= new AsyncDelegateCommand(ExecuteTestSelectAsync);

        private async Task ExecuteTestSelectAsync()
        {
            try
            {
                if (SelectVisionFlow == null) return;
                await Task.Run(() =>
                {
                    SelectVisionFlow.Flow.Reset();

                    while (true)
                    {
                        Thread.Sleep(1);
                        if (SelectedVisionFunction.RunItem(SelectVisionFlow))
                        {
                            return;
                        }
                        else if (SelectVisionFlow.Flow.CheckFlowTime(10))
                        {
                            throw new Exception("流程超时!");

                        }
                    }



                });
            }
            catch (Exception ex)
            {

                SendInfoDialog(ex.Message);
            }


            if (SelectedVisionFunction.Src == null) return;

            ShowMat = SelectedVisionFunction.Src;

 
        }
        private DelegateCommand<string> _Manage;
        /// <summary>
        /// 流程管理
        /// </summary>
        public DelegateCommand<string> Manage =>
            _Manage ??= new DelegateCommand<string>(ExecuteManage);

        void ExecuteManage(string cmd)
        {
            switch (cmd)
            {

                case "删除":

                    if (SelectVisionFlow != null && SelectedVisionFunction.VisionFlows.Contains(SelectVisionFlow))
                    {

                        SelectedVisionFunction.VisionFlows.Remove(SelectVisionFlow);

                    }

                    break;

                case "上移":
                    if (SelectVisionFlow != null && SelectedVisionFunction.VisionFlows.IndexOf(SelectVisionFlow) > 0)
                    {

                        var currentIndex = SelectedVisionFunction.VisionFlows.IndexOf(SelectVisionFlow);
                        SelectedVisionFunction.VisionFlows.Move(currentIndex, currentIndex - 1);

                    }

                    break;

                case "下移":
                    if (SelectVisionFlow != null && SelectedVisionFunction.VisionFlows.IndexOf(SelectVisionFlow) < SelectedVisionFunction.VisionFlows.Count - 1)
                    {

                        var currentIndex = SelectedVisionFunction.VisionFlows.IndexOf(SelectVisionFlow);
                        SelectedVisionFunction.VisionFlows.Move(currentIndex, currentIndex + 1);
                    }


                    break;
            }
        }
        /// <summary>
        /// 添加流程菜单项——按 <see cref="FlowCategoryAttribute"/> 分组
        /// </summary>
        public List<FlowMenuGroup> FlowMenuGroups { get; }

        private static List<FlowMenuGroup> BuildFlowMenuGroups()
        {
            return [.. Enum.GetValues<VisionFlowType>()
                .Select(t => (Type: t, Category: GetCategory(t)))
                .GroupBy(x => x.Category)
                .Select(g => new FlowMenuGroup
                {
                    Header = g.Key,
                    Items = [.. g.Select(x => new FlowMenuItem { Header = x.Type.ToString(), FlowType = x.Type })]
                })];
        }

        private static string GetCategory(VisionFlowType type)
        {
            var field = typeof(VisionFlowType).GetField(type.ToString());
            return field?.GetCustomAttribute<FlowCategoryAttribute>()?.Category ?? "其他";
        }

        private DelegateCommand<string> _AddFlow;
        /// <summary>
        /// 添加Flow
        /// </summary>
        public DelegateCommand<string> AddFlow =>
            _AddFlow ??= new DelegateCommand<string>(ExecuteAddFlow);

        void ExecuteAddFlow(string cmd)
        {
            if (Enum.TryParse(cmd, out VisionFlowType visionFlowType))
            {
                var newFlow = new VisionFlow() { Type = visionFlowType };

                SelectedVisionFunction.VisionFlows.Add(newFlow);

                switch (visionFlowType)
                {
                    case VisionFlowType.从全局图像获取图片:
                        break;
                    case VisionFlowType.从文件获取图片:
                        break;
                    case VisionFlowType.拍照:
                        break;
                    case VisionFlowType.阈值:

                        newFlow.IntParams["ThresholdType"] = 0;
                        newFlow.IntParams["Threshold"] = 100;
                        newFlow.IntParams["MaxValue"] = 255;
                        newFlow.BoolParams["IsOtsu"] = false;
                        newFlow.BoolParams["IsTriangle"] = false;

                        break;
                    case VisionFlowType.GRAY2BGR:
                        break;
                    case VisionFlowType.BGR2GRAY:
                        break;

                    case VisionFlowType.存到全局图像:
                        break;
                    case VisionFlowType.存到文件:
                        break;
                    case VisionFlowType.各通道最小值:
                        break;
                    case VisionFlowType.各通道最大值:
                        break;
                    case VisionFlowType.显示图像到主页:
                        break;
                    case VisionFlowType.卡尺寻边:
                        newFlow.IntParams["Threshold"] = 30;
                        newFlow.IntParams["Direction"] = 0;
                        newFlow.IntParams["EdgeSelector"] = 0;
                        newFlow.IntParams["MinScore"] = 10;
                        newFlow.DoubleParams["RansacTh"] = 5;
                        newFlow.IntParams["CaliperLength"] = 100;
                        newFlow.IntParams["NumCalipers"] = 50;
                        break;
                    case VisionFlowType.卡尺找圆:
                        newFlow.IntParams["Threshold"] = 30;
                        newFlow.IntParams["Direction"] = 0;
                        newFlow.IntParams["CaliperDirection"] = 0;
                        newFlow.IntParams["EdgeSelector"] = 0;
                        newFlow.IntParams["MinInliers"] = 10;
                        newFlow.DoubleParams["RansacTh"] = 5;
                        newFlow.IntParams["MinScore"] = 10;
                        newFlow.IntParams["NumCalipers"] = 50;
                        newFlow.IntParams["CaliperLength"] = 100;
                        break;
                    case VisionFlowType.卡尺找旋转矩形:
                        newFlow.IntParams["Threshold"] = 30;
                        newFlow.IntParams["Direction"] = 0;
                        newFlow.IntParams["CaliperDirection"] = 0;
                        newFlow.IntParams["EdgeSelector"] = 0;
                        newFlow.IntParams["MinInliers"] = 10;
                        newFlow.DoubleParams["RansacTh"] = 5;
                        newFlow.IntParams["MinScore"] = 10;
                        newFlow.IntParams["NumCalipers"] = 50;
                        newFlow.IntParams["CaliperLength"] = 100;
                        break;
                    case VisionFlowType.ORB匹配:
                        newFlow.IntParams["NFeatures"] = 500;
                        newFlow.IntParams["MinMatches"] = 8;
                        newFlow.DoubleParams["MinGoodRatio"] = 0.3;
                        newFlow.StringParams["TemplateName"] = "ORB_Template";
                        break;
                    case VisionFlowType.取通道:
                        newFlow.IntParams["ChannelIndex"] = 0;
                        break;
                    case VisionFlowType.坐标转换:
                        newFlow.DoubleParams["CalibSpacing"] = 5;
                        newFlow.DoubleParams["ImageX"] = 0;
                        newFlow.DoubleParams["ImageY"] = 0;
                        newFlow.StringParams["TransformMat"] = "";
                        break;
                    case VisionFlowType.ROI解码:
                        newFlow.IntParams["DecodeType"] = 0;
                        newFlow.BoolParams["EnableMirror"] = false;
                        newFlow.BoolParams["EnableUpscale"] = true;
                        newFlow.BoolParams["UsePureBarcode"] = false;
                        newFlow.StringParams["ResultVarName"] = "条码解码_Result";
                        break;
                    case VisionFlowType.微信解码:
                        newFlow.BoolParams["EnableMirror"] = false;
                        newFlow.StringParams["ResultVarName"] = "微信解码_Result";
                        break;
                    case VisionFlowType.颜色面积:
                        newFlow.IntParams["HMin"] = 0;
                        newFlow.IntParams["HMax"] = 180;
                        newFlow.IntParams["SMin"] = 0;
                        newFlow.IntParams["SMax"] = 255;
                        newFlow.IntParams["VMin"] = 0;
                        newFlow.IntParams["VMax"] = 255;
                        newFlow.DoubleParams["AreaMinPercent"] = 0;
                        newFlow.DoubleParams["AreaMaxPercent"] = 100;
                        newFlow.StringParams["ResultVarName"] = "颜色面积_Result";
                        break;
                    default:
                        break;
                }


                SelectVisionFlow = null;
                SelectImageData = null;

            }


        }


        #endregion

        #region 其它模型

        /// <summary>
        /// 连接模型
        /// </summary>
        public ConnectsModel Connects { get; set; }
        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }
        /// <summary>
        /// 系统全局变量集合
        /// </summary>
        public ObservableCollection<SystemVariable> SystemVariables { get; set; }

        private CameraBase _SelectedCamera;
        /// <summary>
        /// 选中的相机
        /// </summary>
        public CameraBase SelectedCamera
        {
            get { return _SelectedCamera; }
            set
            {
                SetProperty(ref _SelectedCamera, value);
                if (value != null)
                    SelectVisionFlow.StringParams["Camera"] = value.Name;

            }
        }
        private SystemVariable _SelectedSystemVariable;
        /// <summary>
        /// 选中的全局变量
        /// </summary>
        public SystemVariable SelectedSystemVariable
        {
            get { return _SelectedSystemVariable; }
            set
            {
                SetProperty(ref _SelectedSystemVariable, value);

            }
        }

        private Variable _FlowSelectedVariable;
        /// <summary>
        /// 在流程中选择的全局变量
        /// </summary>
        public Variable FlowSelectedVariable
        {
            get { return _FlowSelectedVariable; }
            set
            {
                SetProperty(ref _FlowSelectedVariable, value);

                if (value != null && SelectVisionFlow != null)
                    SelectVisionFlow.StringParams["Variable"] = value.Name;
            }
        }
 

        private ImageData _SelectImageData;
        /// <summary>
        /// 选择图像数据
        /// </summary>
        public ImageData SelectImageData
        {
            get { return _SelectImageData; }
            set
            {
                SetProperty(ref _SelectImageData, value);
                if (value != null)

                    SelectVisionFlow.StringParams["Image"] = value.Name;


            }
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
        /// 视觉模型
        /// </summary>
        public VisionsModel VisionsModel { get; set; }

        private VisionFunction _SelectedVisionFunction;
        /// <summary>
        /// SelectedVisionFunction
        /// </summary>
        public VisionFunction SelectedVisionFunction
        {
            get { return _SelectedVisionFunction; }
            set { SetProperty(ref _SelectedVisionFunction, value); }
        }

        /// <summary>
        /// Line 类型局部变量列表（供 ComboBox 选择）
        /// </summary>
        public IEnumerable<LocalVariableItem> LineVariables =>
            SelectedVisionFunction?.Params?.Variables?.Where(v => v.VarType == "Line") ?? [];

        public IEnumerable<LocalVariableItem> AllVariables =>
            SelectedVisionFunction?.Params?.Variables ?? [];

        private VisionFlow _SelectVisionFlow;
        /// <summary>
        /// 选择VisionFlow
        /// </summary>
        public VisionFlow SelectVisionFlow
        {
            get { return _SelectVisionFlow; }
            set
            {
                SetProperty(ref _SelectVisionFlow, value);
                ContentRegion = null;
                if (_SelectVisionFlow != null)
                {
                    _SelectVisionFlow_TypeChanged();

                }
            }
        }

        private void _SelectVisionFlow_TypeChanged()
        {
            ContentRegion = null;
            switch (_SelectVisionFlow.Type)
            {
                case VisionFlowType.从全局图像获取图片:
                case VisionFlowType.存到全局图像:
                    ContentRegion = new ImagesPool();
                    break;
                case VisionFlowType.从局部图像获取图片:
                case VisionFlowType.存到局部图像:
                    ContentRegion = new ProcessImageSelector();
                    SelectedMat = null;
                    break;
                case VisionFlowType.从文件获取图片:
                    ContentRegion = new ImageFromFile();
                    break;
                case VisionFlowType.拍照:
                    ContentRegion = new ImageFromCamera();

                    break;
                case VisionFlowType.阈值:
                    ContentRegion = new Threshold();
                    break;
                case VisionFlowType.GRAY2BGR:
                    break;
                case VisionFlowType.BGR2GRAY:
                    break;
                case VisionFlowType.存到文件:
                    ContentRegion = new ImageToFile();
                    break;
                case VisionFlowType.各通道最小值:
                    break;
                case VisionFlowType.各通道最大值:
                    break;
                case VisionFlowType.显示图像到主页:
                    ContentRegion = new ImageToHomePage();
                    break;
                case VisionFlowType.卡尺寻边:
                    ContentRegion = new CaliperEdgeFind();
                    break;
                case VisionFlowType.卡尺找圆:
                    ContentRegion = new CaliperFindCircle();
                    break;
                case VisionFlowType.卡尺找旋转矩形:
                    ContentRegion = new CaliperFindRect();
                    break;
                case VisionFlowType.两线交点:
                    ContentRegion = new TwoLineIntersect();
                    break;
                case VisionFlowType.ORB匹配:
                    ContentRegion = new ORBMatch();
                    break;
                case VisionFlowType.取通道:
                    ContentRegion = new SplitChannel();
                    break;
                case VisionFlowType.坐标转换:
                    ContentRegion = new CoordTransform();
                    break;
                case VisionFlowType.ROI解码:
                    ContentRegion = new BarcodeDecode();
                    break;
                case VisionFlowType.微信解码:
                    ContentRegion = new WeChatDecode();
                    break;
                case VisionFlowType.颜色面积:
                    ContentRegion = new ColorArea();
                    break;

            }

            if (ContentRegion != null)
                ContentRegion.DataContext = this;
        }


        #endregion
 
        #region 局部图像管理

        private DelegateCommand<string> _MatManage;
        /// <summary>
        /// 局部图像管理
        /// </summary>
        public DelegateCommand<string> MatManage =>
            _MatManage ??= new DelegateCommand<string>(ExecuteMatManage);

        void ExecuteMatManage(string cmd)
        {
            if (SelectedVisionFunction == null) return;
            switch (cmd)
            {
                case "New":
                    var baseName = "temp";
                    int index = 1;
                    while (SelectedVisionFunction.Params.Mats.Where(w => w.Name == $"{baseName}{index}").Any())
                        index++;
                    SelectedVisionFunction.Params.Mats.Add(new ImageData { Name = $"{baseName}{index}" });
                    break;
                case "Remove":
                    if (GridSelectedMat == null) return;
                    if (SelectedVisionFunction.Params.Mats.Contains(GridSelectedMat))
                        SelectedVisionFunction.Params.Mats.Remove(GridSelectedMat);
                    break;
                case "Show":
                    if (GridSelectedMat.Mat != null && GridSelectedMat.Mat.Empty() == false)
                        ShowMat = GridSelectedMat.Mat;
                    break;

            }
        }

        private ImageData _SelectedMat;
        /// <summary>
        /// 在流程中选择的全局图像
        /// </summary>
        public ImageData SelectedMat
        {
            get { return _SelectedMat; }
            set
            {
                SetProperty(ref _SelectedMat, value);
                if (value != null)

                    SelectVisionFlow.StringParams["Image"] = value.Name;
            }
        }
        private ImageData _GridSelectedMat;
        /// <summary>
        /// 在表格中选择的全局图像
        /// </summary>
        public ImageData GridSelectedMat
        {
            get { return _GridSelectedMat; }
            set { SetProperty(ref _GridSelectedMat, value); }
        }
        #endregion

        #region 变量表

        private LocalVariableItem _SelectedLocalVariable;
        /// <summary>
        /// 局部变量表中选中的行
        /// </summary>
        public LocalVariableItem SelectedLocalVariable
        {
            get { return _SelectedLocalVariable; }
            set { SetProperty(ref _SelectedLocalVariable, value); }
        }

        private DelegateCommand<string> _LocalVariableManage;
        /// <summary>
        /// 局部变量表右键菜单命令（AddPoint / AddLine / AddCircle / AddRect / Remove）
        /// </summary>
        public DelegateCommand<string> LocalVariableManage =>
            _LocalVariableManage ??= new DelegateCommand<string>(ExecuteLocalVariableManage);

        void ExecuteLocalVariableManage(string cmd)
        {
            if (SelectedVisionFunction == null) return;
            var vars = SelectedVisionFunction.Params.Variables;

            switch (cmd)
            {
                case "Edit":
                    if (_SelectedLocalVariable != null)
                    {
                        _dialogService.ShowDialog("VariableEditor", new DialogParameters { { "item", _SelectedLocalVariable } }, _ =>
                        {

                        });
                    }
                    break;
                case "AddPoint":
                    vars.Add(new LocalVariableItem(NextVarName(vars, "point"), "Pos", new Pos()));
                    break;
                case "AddLine":
                    vars.Add(new LocalVariableItem(NextVarName(vars, "line"), "Line", new Line()));
                    break;
                case "AddCircle":
                    vars.Add(new LocalVariableItem(NextVarName(vars, "circle"), "Circle", new Circle()));
                    break;
                case "AddRect":
                    vars.Add(new LocalVariableItem(NextVarName(vars, "rect"), "Rect", new Rect()));
                    break;
                case "Remove":
                    if (_SelectedLocalVariable != null && vars.Contains(_SelectedLocalVariable))
                        vars.Remove(_SelectedLocalVariable);
                    break;
            }
        }

        private static string NextVarName(ObservableCollection<LocalVariableItem> vars, string baseName)
        {
            var existing = vars.Select(v => v.Name).ToHashSet();
            int i = 1;
            while (existing.Contains($"{baseName}{i}"))
                i++;
            return $"{baseName}{i}";
        }
        private DelegateCommand<object> _SystemVariablesManage;
        public DelegateCommand<object> SystemVariablesManage =>
            _SystemVariablesManage ??= new DelegateCommand<object>(ExecuteVariablesManage);

        void ExecuteVariablesManage(object param)
        {
            var cmd = param as string;
            switch (cmd)
            {
                case "New":
                    SystemVariables.Add(new() { RecipeID = GlobalModel.CurrentRecipe.ID, _DatasContext = GlobalModel._DatasContext });
                    break;
                case "Delete":

                    if (SelectedSystemVariable != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除  [{SelectedSystemVariable.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        GlobalModel._DatasContext.SystemVariables.Remove(SelectedSystemVariable);
                        SystemVariables.Remove(SelectedSystemVariable);
                        var name = SelectedSystemVariable.Name;
                        SendInfoDialog($"已删除变量：{name}");
                    }
                    break;
                case "Save":

                    var names = new List<string>();

                    foreach (var item in SystemVariables)
                    {
                        if (string.IsNullOrEmpty(item.Name))
                        {
                            SendInfoDialog("保存失败，无变量名！");
                            return;
                        }

                        if (names.Contains(item.Name))
                        {

                            SendInfoDialog("保存失败，重名的变量！");
                            return;

                        }
                        else
                        {
                            names.Add(item.Name);
                        }

                    }


                    foreach (var item in SystemVariables)
                    {
                        if (!GlobalModel._DatasContext.SystemVariables.Contains(item))
                        {
                            GlobalModel._DatasContext.SystemVariables.Add(item);
                        }

                    }
                    GlobalModel._DatasContext.Save();

                    SendInfoDialog("保存成功！");
                    break;
            }

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
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
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

        #region  公共指令


        private DelegateCommand<string> _ImageFile;
        /// <summary>
        /// 图像文件
        /// </summary>
        public DelegateCommand<string> ImageFile =>
            _ImageFile ??= new DelegateCommand<string>(ExecuteImageFile);

        void ExecuteImageFile(string cmd)
        {
            switch (cmd)
            {
                case "Open":
                    {
                        Microsoft.Win32.OpenFileDialog ofd = new()
                        {
                            DefaultExt = ".*",
                            Filter = "图像文件(*.jpg;*.png;*.bmp;*.tiff)|*.jpg;*.png;*.bmp;*.tiff"
                        };
                        if (ofd.ShowDialog() == true)
                        {
                            try
                            {
                                SelectedVisionFunction.Src = Cv2.ImRead(ofd.FileName);
                                if (SelectedVisionFunction.Src != null && !SelectedVisionFunction.Src.Empty())
                                {
                                    SelectVisionFlow.StringParams["Path"] = ofd.FileName;

                                    ShowMat = SelectedVisionFunction.Src;
                                }
                                else
                                    SendInfoDialog("不支持的图像格式");
                            }
                            catch (Exception ex)
                            {
                                SendInfoDialog($"读取图像异常 - {ex.Message}");

                            }
                        }
                    }
                    break;
                case "Save":
                    {

                        Microsoft.Win32.OpenFileDialog openFileDialog = new()
                        {
                            Title = "选择文件夹",
                            Filter = "文件夹|*.directory",
                            FileName = "选择此文件夹",

                            ValidateNames = false,
                            CheckFileExists = false,
                            CheckPathExists = true,
                            Multiselect = true,//允许同时选择多个文件
                            InitialDirectory = "D:"//指定启动路径
                        };
                        if (openFileDialog.ShowDialog() == true)

                        {
                            var path = openFileDialog.FileName.Replace("选择此文件夹.directory", "");

                            if (!System.IO.Directory.Exists(path))
                            {
                                System.Windows.MessageBox.Show(path + "文件夹不存在", "选择文件提示");
                                return;
                            }


                            SelectVisionFlow.StringParams["Path"] = path;
                        }
                    }


                    break;
            }
        }


  




        private AsyncDelegateCommand _DrawFindEdgeROI;
        /// <summary>
        /// 绘制查找边缘ROI
        /// </summary>
        public AsyncDelegateCommand DrawFindEdgeROI =>
            _DrawFindEdgeROI ??= new AsyncDelegateCommand(ExecuteDrawFindEdgeROIAsync);

        private async Task ExecuteDrawFindEdgeROIAsync()
        {
            // 从活动窗口向下遍历视觉树找到 ImageEdit 控件
            var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive);
            var imageEdit = WpfTool.FindVisualChild<ImageEdit>(window);
            if (imageEdit?.ImageSource == null)
            {
                SendInfoDialog("请先获取图片！");
                return;
            }   

            try
            {
                var roi = await imageEdit.DrawLineAsync("卡尺寻边");
                if (roi == null) return;

                // 保存直线参数到流程步骤
                if (SelectVisionFlow == null) return;
                SelectVisionFlow.DoubleParams["LineStartX"] = roi.StartX;
                SelectVisionFlow.DoubleParams["LineStartY"] = roi.StartY;
                SelectVisionFlow.DoubleParams["LineEndX"] = roi.EndX;
                SelectVisionFlow.DoubleParams["LineEndY"] = roi.EndY;

                // 在图片上画出箭头线 + 端点
                var line = new System.Windows.Shapes.Line
                {
                    X1 = roi.StartX,
                    Y1 = roi.StartY,
                    X2 = roi.EndX,
                    Y2 = roi.EndY,
                    Stroke = Brushes.Lime,
                    StrokeThickness = 2,
                    Tag = "卡尺寻边",
                };
                imageEdit.Draw(line);

                // 箭头
                double adx = roi.EndX - roi.StartX, ady = roi.EndY - roi.StartY;
                double alen = Math.Sqrt(adx * adx + ady * ady);
                if (alen > 3)
                {
                    double aux = adx / alen, auy = ady / alen;
                    const double headSize = 14;
                    var arrow = new Polygon
                    {
                        Fill = Brushes.Lime,
                        Points = new PointCollection
                        {
                            new System.Windows.Point(roi.EndX, roi.EndY),
                            new System.Windows.Point(roi.EndX - headSize * (aux * 0.7 - auy * 0.7),
                                       roi.EndY - headSize * (auy * 0.7 + aux * 0.7)),
                            new System.Windows.Point(roi.EndX - headSize * (aux * 0.7 + auy * 0.7),
                                       roi.EndY - headSize * (auy * 0.7 - aux * 0.7)),
                        },
                        Tag = "卡尺寻边",
                    };
                    imageEdit.Draw(arrow);
                }

                var epStart = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White,
                    Stroke = Brushes.Lime,
                    StrokeThickness = 2,
                    Tag = "卡尺寻边",
                };
                imageEdit.Draw(epStart, (int)(roi.StartY - 4), (int)(roi.StartX - 4));

                var epEnd = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White,
                    Stroke = Brushes.Lime,
                    StrokeThickness = 2,
                    Tag = "卡尺寻边",
                };
                imageEdit.Draw(epEnd, (int)(roi.EndY - 4), (int)(roi.EndX - 4));

                SendInfoDialog("直线已保存");
            }
            catch (Exception ex)
            {
                SendInfoDialog($"直线绘制失败：{ex.Message}");
            }
        }


        private AsyncDelegateCommand _ComputeEdgeTemplate;
        public AsyncDelegateCommand ComputeEdgeTemplate =>
            _ComputeEdgeTemplate ??= new AsyncDelegateCommand(ExecuteComputeEdgeTemplateAsync);

        private async Task ExecuteComputeEdgeTemplateAsync()
        {
            if (SelectVisionFlow == null || SelectedVisionFunction == null) return;
            SelectVisionFlow.StringParams["ComputeTemplate"] = "1";
            try
            {
                if (!SelectedVisionFunction.RunItem(SelectVisionFlow))
                    SendInfoDialog("模板计算失败");
                else
                    SendInfoDialog("模板已保存");
            }
            catch (Exception ex) { SendInfoDialog(ex.Message); }
            finally { SelectVisionFlow.StringParams.Remove("ComputeTemplate"); }
        }


        private AsyncDelegateCommand _DrawFindCircleROI;
        /// <summary>
        /// 绘制找圆ROI
        /// </summary>
        public AsyncDelegateCommand DrawFindCircleROI =>
            _DrawFindCircleROI ??= new AsyncDelegateCommand(ExecuteDrawFindCircleROIAsync);

        private async Task ExecuteDrawFindCircleROIAsync()
        {
            var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive);
            var imageEdit = WpfTool.FindVisualChild<ImageEdit>(window);
            if (imageEdit?.ImageSource == null)
            {
                SendInfoDialog("请先获取图片！");
                return;
            }

            try
            {
                var roi = await imageEdit.DrawCircleAsync("卡尺找圆");
                if (roi == null) return;

                if (SelectVisionFlow == null) return;

                SelectVisionFlow.DoubleParams["CircleCenterX"] = roi.CenterX;
                SelectVisionFlow.DoubleParams["CircleCenterY"] = roi.CenterY;
                SelectVisionFlow.DoubleParams["CircleRadius"] = roi.Radius;

                // 画圆形边框
                var ellipse = new System.Windows.Shapes.Ellipse
                {
                    Width = roi.Radius * 2,
                    Height = roi.Radius * 2,
                    Stroke = Brushes.Magenta,
                    StrokeThickness = 2,
                    Tag = "卡尺找圆",
                };
                Canvas.SetLeft(ellipse, roi.CenterX - roi.Radius);
                Canvas.SetTop(ellipse, roi.CenterY - roi.Radius);
                imageEdit.Draw(ellipse);

                // 画中心十字
                double cx = roi.CenterX, cy = roi.CenterY;
                var crossH = new System.Windows.Shapes.Line
                {
                    X1 = cx - 10,
                    Y1 = cy,
                    X2 = cx + 10,
                    Y2 = cy,
                    Stroke = Brushes.Magenta,
                    StrokeThickness = 1,
                    Tag = "卡尺找圆",
                };
                imageEdit.Draw(crossH);
                var crossV = new System.Windows.Shapes.Line
                {
                    X1 = cx,
                    Y1 = cy - 10,
                    X2 = cx,
                    Y2 = cy + 10,
                    Stroke = Brushes.Magenta,
                    StrokeThickness = 1,
                    Tag = "卡尺找圆",
                };
                imageEdit.Draw(crossV);

                // 画半径指示线（向右）
                var radiusLine = new System.Windows.Shapes.Line
                {
                    X1 = cx,
                    Y1 = cy,
                    X2 = cx + roi.Radius,
                    Y2 = cy,
                    Stroke = Brushes.Magenta,
                    StrokeThickness = 1,
                    StrokeDashArray = new System.Windows.Media.DoubleCollection { 3, 3 },
                    Tag = "卡尺找圆",
                };
                imageEdit.Draw(radiusLine);

                SendInfoDialog("圆形ROI已保存");
            }
            catch (Exception ex)
            {
                SendInfoDialog($"圆形绘制失败：{ex.Message}");
            }
        }


        private AsyncDelegateCommand _DrawFindRectROI;
        /// <summary>
        /// 绘制找旋转矩形ROI
        /// </summary>
        public AsyncDelegateCommand DrawFindRectROI =>
            _DrawFindRectROI ??= new AsyncDelegateCommand(ExecuteDrawFindRectROIAsync);

        private async Task ExecuteDrawFindRectROIAsync()
        {
            var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive);
            var imageEdit = WpfTool.FindVisualChild<ImageEdit>(window);
            if (imageEdit?.ImageSource == null)
            {
                SendInfoDialog("请先获取图片！");
                return;
            }

            try
            {
                var roi = await imageEdit.DrawRotateRectROIAsync("卡尺找旋转矩形");
                if (roi == null) return;

                if (SelectVisionFlow == null) return;

                SelectVisionFlow.DoubleParams["RectCenterX"] = roi.CenterX;
                SelectVisionFlow.DoubleParams["RectCenterY"] = roi.CenterY;
                SelectVisionFlow.DoubleParams["RectWidth"] = roi.RectWidth;
                SelectVisionFlow.DoubleParams["RectHeight"] = roi.RectHeight;
                SelectVisionFlow.DoubleParams["RectAngle"] = roi.RectAngle;

                SendInfoDialog("旋转矩形ROI已保存");
            }
            catch (Exception ex)
            {
                SendInfoDialog($"旋转矩形绘制失败：{ex.Message}");
            }
        }


        private AsyncDelegateCommand _CalibrateTransform;
        public AsyncDelegateCommand CalibrateTransform =>
            _CalibrateTransform ??= new AsyncDelegateCommand(ExecuteCalibrateTransformAsync);

        private async Task ExecuteCalibrateTransformAsync()
        {
            var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive);
            var imageEdit = WpfTool.FindVisualChild<ImageEdit>(window);
            if (imageEdit?.ImageSource == null) { SendInfoDialog("请先获取图片！"); return; }

            var src = SelectedVisionFunction?.Src;
            if (src == null || src.Empty()) { SendInfoDialog("请先获取图片！"); return; }

            try
            {
                var roi = await imageEdit.DrawROIAsync("坐标转换标定");
                if (roi == null || SelectVisionFlow == null) return;

                double rowBase = SelectVisionFlow.DoubleParams.TryGetValue("CalibRowBase", out double rb) ? rb : 0;
                double colBase = SelectVisionFlow.DoubleParams.TryGetValue("CalibColBase", out double cb) ? cb : 0;
                double spacing = SelectVisionFlow.DoubleParams.TryGetValue("CalibSpacing", out double sp) ? Math.Max(sp, 0.1) : 5;
                string matName = SelectVisionFlow.StringParams.TryGetValue("CalibMatName", out var mn) && !string.IsNullOrEmpty(mn) ? mn : "标定矩阵";

                Mat gray;
                if (src.Channels() == 3)
                {
                    gray = new Mat();
                    Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    gray = src;
                }
                int x = Math.Clamp((int)roi.Left, 0, gray.Width - 1);
                int y = Math.Clamp((int)roi.Top, 0, gray.Height - 1);
                int w = Math.Min((int)roi.Width, gray.Width - x);
                int h = Math.Min((int)roi.Height, gray.Height - y);
                if (w < 20 || h < 20) { SendInfoDialog("ROI 区域太小！"); return; }

                using var roiMat = gray[new OpenCvSharp.Rect(x, y, w, h)];
                using var bin = new Mat();
                Cv2.Threshold(roiMat, bin, 128, 255, ThresholdTypes.Otsu | ThresholdTypes.BinaryInv);
                Cv2.FindContours(bin, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                var dots = new List<(float cx, float cy, float r)>();
                foreach (var c in contours)
                {
                    double area = Cv2.ContourArea(c);
                    if (area < 10 || area > w * h * 0.3) continue;
                    var mu = Cv2.Moments(c);
                    if (mu.M00 == 0) continue;
                    float cx = (float)(mu.M10 / mu.M00 + x);
                    float cy = (float)(mu.M01 / mu.M00 + y);
                    float r = (float)Math.Sqrt(area / Math.PI);
                    dots.Add((cx, cy, r));
                }

                SelectedVisionFunction.Params.ResultDoubles["CalibDotCount"] = dots.Count;
                if (dots.Count != 9) { SendInfoDialog($"期望找到9个圆点，实际找到 {dots.Count} 个"); return; }

                var sorted = dots.OrderBy(d => d.cy).ToList();
                var rows = new List<List<(float cx, float cy, float r)>>();
                for (int i = 0; i < 9; i += 3)
                    rows.Add(sorted.Skip(i).Take(3).OrderBy(d => d.cx).ToList());

                // 画圆
                if (src.Channels() == 1) { Cv2.CvtColor(src, src, ColorConversionCodes.GRAY2BGR); }
                int idx = 0;
                foreach (var row in rows)
                    foreach (var (cx, cy, r) in row)
                    {
                        for (int d = 0; d < 360; d += 9)
                        {
                            double rad = d * Math.PI / 180;
                            Cv2.Circle(src, new OpenCvSharp.Point((int)(cx + r * Math.Cos(rad)), (int)(cy + r * Math.Sin(rad))), 1, Scalar.Magenta, -1);
                        }
                        Cv2.PutText(src, idx.ToString(), new OpenCvSharp.Point((int)cx + (int)r + 4, (int)cy - (int)r - 4), HersheyFonts.HersheySimplex, 0.8, Scalar.Magenta, 2);
                        idx++;
                    }

                ShowMat = src;

                var imgPts = new List<Point2f>();
                var worldPts = new List<Point2f>();
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                    {
                        imgPts.Add(new Point2f(rows[r][c].cx, rows[r][c].cy));
                        worldPts.Add(new Point2f((float)(colBase + c * spacing), (float)(rowBase + r * spacing)));
                    }

                using Mat noInliers = new Mat();
                var affine = Cv2.EstimateAffine2D(InputArray.Create(imgPts), InputArray.Create(worldPts), noInliers, RobustEstimationAlgorithms.RANSAC, 3.0);
                if (affine == null || affine.Empty()) { SendInfoDialog("计算变换矩阵失败！"); return; }

                string json = MatExtension.SerializeAffineMat(affine);
                affine.Dispose();
                if (gray != src) gray.Dispose();

                var existing = SystemVariables.FirstOrDefault(v => v.Name == matName);
                if (existing != null)
                {
                    existing.RecipeID = VisionsModel.GlobalModel.CurrentRecipe.ID;
                    existing.Value = json;
                    existing.DefaultValue = json;
                    existing.RetainPersistent = false;
                }
                else
                {
                    SystemVariables.Add(new SystemVariable
                    {
                        RecipeID = VisionsModel.GlobalModel.CurrentRecipe.ID,
                        Name = matName,
                        Type = VariableDataType.STRING,
                        Value = json,
                        DefaultValue = json,
                        RetainPersistent = false
                    });
                }
                SystemVariablesManage.Execute("Save");
                SendInfoDialog("标定成功");
            }
            catch (Exception ex) { SendInfoDialog(ex.Message); }
        }    
     
     

        private AsyncDelegateCommand _ComputeTwoLineTemplate;
        public AsyncDelegateCommand ComputeTwoLineTemplate =>
            _ComputeTwoLineTemplate ??= new AsyncDelegateCommand(ExecuteComputeTwoLineTemplateAsync);
        private async Task ExecuteComputeTwoLineTemplateAsync()
        {
            if (SelectVisionFlow == null || SelectedVisionFunction == null) return;
            SelectVisionFlow.StringParams["ComputeTemplate"] = "1";
            try
            {
                if (!SelectedVisionFunction.RunItem(SelectVisionFlow))
                    SendInfoDialog("模板计算失败");
                else
                    SendInfoDialog("模板已保存");
            }
            catch (Exception ex)
            {
                SendInfoDialog(ex.Message);
            }
            finally
            {
                SelectVisionFlow.StringParams.Remove("ComputeTemplate");
            }
        }


        private AsyncDelegateCommand _ComputeCircleTemplate;
        public AsyncDelegateCommand ComputeCircleTemplate =>
            _ComputeCircleTemplate ??= new AsyncDelegateCommand(ExecuteComputeCircleTemplateAsync);

        private async Task ExecuteComputeCircleTemplateAsync()
        {
            if (SelectVisionFlow == null || SelectedVisionFunction == null) return;
            SelectVisionFlow.StringParams["ComputeTemplate"] = "1";
            try
            {
                if (!SelectedVisionFunction.RunItem(SelectVisionFlow))
                    SendInfoDialog("模板计算失败");
                else
                    SendInfoDialog("模板已保存");
            }
            catch (Exception ex)
            {
                SendInfoDialog(ex.Message);
            }
            finally
            {
                SelectVisionFlow.StringParams.Remove("ComputeTemplate");
            }
        }


        private AsyncDelegateCommand _ComputeRectTemplate;
        /// <summary>
        /// 计算矩形模板位置
        /// </summary>
        public AsyncDelegateCommand ComputeRectTemplate =>
            _ComputeRectTemplate ??= new AsyncDelegateCommand(ExecuteComputeRectTemplateAsync);

        private async Task ExecuteComputeRectTemplateAsync()
        {
            if (SelectVisionFlow == null || SelectedVisionFunction == null)
                return;

            // 设标记让 handler 保存模板
            SelectVisionFlow.StringParams["ComputeTemplate"] = "1";

            try
            {
                // 执行一次找矩形
                if (!SelectedVisionFunction.RunItem(SelectVisionFlow))
                {
                    SendInfoDialog("模板计算失败");
                }
                else
                {
                    SendInfoDialog("模板已保存");
                }
            }
            catch (Exception ex)
            {
                SendInfoDialog(ex.Message);
            }
            finally
            {
                SelectVisionFlow.StringParams.Remove("ComputeTemplate");
            }
        }


        private AsyncDelegateCommand _DrawORBTemplateROI;
        /// <summary>
        /// 绘制ORB模板ROI
        /// </summary>
        public AsyncDelegateCommand DrawORBTemplateROI =>
            _DrawORBTemplateROI ??= new AsyncDelegateCommand(ExecuteDrawORBTemplateROIAsync);

        private async Task ExecuteDrawORBTemplateROIAsync()
        {
            var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive);
            var imageEdit = WpfTool.FindVisualChild<ImageEdit>(window);
            if (imageEdit?.ImageSource == null)
            {
                SendInfoDialog("请先获取图片！");
                return;
            }

            try
            {
                if (SelectVisionFlow == null) return;

                // 读取用户选择的形状
                string roiShape = SelectVisionFlow.StringParams.TryGetValue("ROIShape", out var rs)
                    && !string.IsNullOrEmpty(rs) ? rs : "矩形";

                // 根据形状调用不同的 ROI 绘制接口
                double centerX = 0, centerY = 0;
                double templateW = 0, templateH = 0;
                double templateAngle = 0;

                switch (roiShape)
                {
                    case "矩形":
                        {
                            var roi = await imageEdit.DrawROIAsync("ORB匹配");
                            if (roi == null) return;

                            SelectVisionFlow.DoubleParams["TemplateLeft"] = roi.Left;
                            SelectVisionFlow.DoubleParams["TemplateTop"] = roi.Top;
                            SelectVisionFlow.DoubleParams["TemplateWidth"] = roi.Width;
                            SelectVisionFlow.DoubleParams["TemplateHeight"] = roi.Height;

                            centerX = roi.Left + roi.Width / 2.0;
                            centerY = roi.Top + roi.Height / 2.0;
                            templateW = roi.Width;
                            templateH = roi.Height;

                            // 从源图中截取矩形区域
                            var srcMat = SelectedVisionFunction.Src;
                            if (srcMat != null && !srcMat.Empty())
                            {
                                int x = Math.Clamp((int)roi.Left, 0, srcMat.Width - 1);
                                int y = Math.Clamp((int)roi.Top, 0, srcMat.Height - 1);
                                int w = Math.Min((int)roi.Width, srcMat.Width - x);
                                int h = Math.Min((int)roi.Height, srcMat.Height - y);
                                if (w > 0 && h > 0)
                                    SaveTemplateMat(srcMat[new OpenCvSharp.Rect(x, y, w, h)].Clone());
                            }

                            // 画矩形边框
                            DrawRectVisual(imageEdit, roi.Left, roi.Top, roi.Width, roi.Height);
                            break;
                        }

                    case "旋转矩形":
                        {
                            var roi = await imageEdit.DrawRotateRectROIAsync("ORB匹配");
                            if (roi == null) return;

                            SelectVisionFlow.DoubleParams["TemplateCenterX"] = roi.CenterX;
                            SelectVisionFlow.DoubleParams["TemplateCenterY"] = roi.CenterY;
                            SelectVisionFlow.DoubleParams["TemplateWidth"] = roi.RectWidth;
                            SelectVisionFlow.DoubleParams["TemplateHeight"] = roi.RectHeight;
                            SelectVisionFlow.DoubleParams["TemplateAngle"] = roi.RectAngle;

                            centerX = roi.CenterX;
                            centerY = roi.CenterY;
                            templateW = roi.RectWidth;
                            templateH = roi.RectHeight;
                            templateAngle = roi.RectAngle;

                            // 截取旋转矩形外接矩形（完全包含旋转矩形），外部置黑
                            var srcMat = SelectedVisionFunction.Src;
                            if (srcMat != null && !srcMat.Empty())
                            {
                                double cx = roi.CenterX, cy = roi.CenterY;
                                double w = roi.RectWidth, h = roi.RectHeight;
                                double rad = roi.RectAngle * Math.PI / 180.0;
                                double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
                                double hw = w / 2.0, hh = h / 2.0;
                                // 计算旋转矩形 4 个角点
                                Point2f[] corners =
                                [
                                    new((float)(cx + (-hw * cosA - (-hh) * sinA)), (float)(cy + (-hw * sinA + (-hh) * cosA))),
                                new((float)(cx + ( hw * cosA - (-hh) * sinA)), (float)(cy + ( hw * sinA + (-hh) * cosA))),
                                new((float)(cx + ( hw * cosA -  hh * sinA)), (float)(cy + ( hw * sinA +  hh * cosA))),
                                new((float)(cx + (-hw * cosA -  hh * sinA)), (float)(cy + (-hw * sinA +  hh * cosA))),
                            ];
                                float minX = corners.Min(p => p.X), maxX = corners.Max(p => p.X);
                                float minY = corners.Min(p => p.Y), maxY = corners.Max(p => p.Y);
                                int bx = Math.Clamp((int)Math.Floor(minX), 0, srcMat.Width - 1);
                                int by = Math.Clamp((int)Math.Floor(minY), 0, srcMat.Height - 1);
                                int bw = Math.Min((int)Math.Ceiling(maxX) - bx, srcMat.Width - bx);
                                int bh = Math.Min((int)Math.Ceiling(maxY) - by, srcMat.Height - by);
                                if (bw > 0 && bh > 0)
                                {
                                    // 保存外接矩形位置，供 handler 放回原尺寸图像
                                    SelectVisionFlow.DoubleParams["TemplateBBoxLeft"] = bx;
                                    SelectVisionFlow.DoubleParams["TemplateBBoxTop"] = by;
                                    SelectVisionFlow.DoubleParams["TemplateBBoxWidth"] = bw;
                                    SelectVisionFlow.DoubleParams["TemplateBBoxHeight"] = bh;

                                    using var cropped = srcMat[new OpenCvSharp.Rect(bx, by, bw, bh)].Clone();
                                    // 创建旋转矩形遮罩，外部置黑
                                    using var mask = new Mat(cropped.Size(), MatType.CV_8UC1, Scalar.Black);
                                    var rr = new RotatedRect(
                                        new Point2f((float)(cx - bx), (float)(cy - by)),
                                        new Size2f((float)w, (float)h),
                                        (float)roi.RectAngle);
                                    var rrPts = rr.Points();
                                    Cv2.FillPoly(mask, [rrPts.Select(p => new OpenCvSharp.Point((int)p.X, (int)p.Y)).ToArray()], Scalar.White);
                                    Mat result = Mat.Zeros(cropped.Size(), cropped.Type());
                                    cropped.CopyTo(result, mask);
                                    SaveTemplateMat(result);
                                }
                            }

                            // 画旋转矩形边框（直接用 ImageEdit 已有的视觉）
                            break;
                        }

                    case "圆形":
                        {
                            var roi = await imageEdit.DrawCircleAsync("ORB匹配");
                            if (roi == null) return;

                            SelectVisionFlow.DoubleParams["TemplateCenterX"] = roi.CenterX;
                            SelectVisionFlow.DoubleParams["TemplateCenterY"] = roi.CenterY;
                            SelectVisionFlow.DoubleParams["TemplateRadius"] = roi.Radius;
                            SelectVisionFlow.DoubleParams["TemplateWidth"] = roi.Radius * 2;
                            SelectVisionFlow.DoubleParams["TemplateHeight"] = roi.Radius * 2;

                            centerX = roi.CenterX;
                            centerY = roi.CenterY;
                            templateW = roi.Radius * 2;
                            templateH = roi.Radius * 2;

                            // 截取圆形外接正方形
                            var srcMat = SelectedVisionFunction.Src;
                            if (srcMat != null && !srcMat.Empty())
                            {
                                double r = roi.Radius;
                                int x = Math.Clamp((int)(roi.CenterX - r), 0, srcMat.Width - 1);
                                int y = Math.Clamp((int)(roi.CenterY - r), 0, srcMat.Height - 1);
                                int size = Math.Min((int)(r * 2), Math.Min(srcMat.Width - x, srcMat.Height - y));
                                if (size > 0)
                                {
                                    // 裁剪外接正方形
                                    using var cropped = srcMat[new OpenCvSharp.Rect(x, y, size, size)].Clone();
                                    // 创建圆形遮罩，外部置黑
                                    using var mask = new Mat(cropped.Size(), MatType.CV_8UC1, Scalar.Black);
                                    Cv2.Circle(mask, new OpenCvSharp.Point(size / 2, size / 2), size / 2, Scalar.White, -1);
                                    Mat result = Mat.Zeros(cropped.Size(), cropped.Type());
                                    cropped.CopyTo(result, mask);
                                    SaveTemplateMat(result);
                                }
                            }

                            // 画圆形边框
                            DrawCircleVisual(imageEdit, roi.CenterX, roi.CenterY, roi.Radius);
                            break;
                        }
                }

                // 保存公共参数
                SelectVisionFlow.DoubleParams["Width"] = imageEdit.ImageSource.Width;
                SelectVisionFlow.DoubleParams["Height"] = imageEdit.ImageSource.Height;
                SelectVisionFlow.DoubleParams["TemplateCenterX"] = centerX;
                SelectVisionFlow.DoubleParams["TemplateCenterY"] = centerY;

                SendInfoDialog("模板已保存");
            }
            catch (Exception ex)
            {
                SendInfoDialog($"模板绘制失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 保存模板 Mat 到 Params（替换同名旧模板）
        /// </summary>
        private void SaveTemplateMat(Mat templateMat)
        {
            string templateName = SelectVisionFlow.StringParams.TryGetValue("TemplateName", out var tn)
                                  && !string.IsNullOrEmpty(tn) ? tn : "ORB_Template";

            var existing = SelectedVisionFunction.Params.Mats
                .FirstOrDefault(m => m.Name == templateName);
            if (existing != null)
            {
                existing.Mat?.Dispose();
                SelectedVisionFunction.Params.Mats.Remove(existing);
            }

            SelectedVisionFunction.Params.Mats.Add(new ImageData
            {
                Name = templateName,
                Mat = templateMat
            });
        }

        private AsyncDelegateCommand _DrawBarcodeROI;
        /// <summary>
        /// 绘制条码解码 ROI 区域
        /// </summary>
        public AsyncDelegateCommand DrawBarcodeROI =>
            _DrawBarcodeROI ??= new AsyncDelegateCommand(ExecuteDrawBarcodeROIAsync);

        private async Task ExecuteDrawBarcodeROIAsync()
        {
            var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive);
            var imageEdit = WpfTool.FindVisualChild<ImageEdit>(window);
            if (imageEdit?.ImageSource == null)
            {
                SendInfoDialog("请先获取图片！");
                return;
            }

            try
            {
                var roi = await imageEdit.DrawROIAsync("条码解码");
                if (roi == null) return;

                if (SelectVisionFlow == null) return;

                SelectVisionFlow.DoubleParams["ROILeft"] = roi.Left;
                SelectVisionFlow.DoubleParams["ROITop"] = roi.Top;
                SelectVisionFlow.DoubleParams["ROIWidth"] = roi.Width;
                SelectVisionFlow.DoubleParams["ROIHeight"] = roi.Height;

                // 画矩形边框
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = roi.Width,
                    Height = roi.Height,
                    Stroke = Brushes.Lime,
                    StrokeThickness = 2,
                    StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                    Tag = "条码解码",
                };
                Canvas.SetLeft(rect, roi.Left);
                Canvas.SetTop(rect, roi.Top);
                imageEdit.Draw(rect);

                SendInfoDialog("条码解码 ROI 已保存");
            }
            catch (Exception ex)
            {
                SendInfoDialog($"ROI 绘制失败：{ex.Message}");
            }
        }

        private AsyncDelegateCommand _DrawColorAreaROI;
        /// <summary>
        /// 绘制颜色面积 ROI 区域
        /// </summary>
        public AsyncDelegateCommand DrawColorAreaROI =>
            _DrawColorAreaROI ??= new AsyncDelegateCommand(ExecuteDrawColorAreaROIAsync);

        private async Task ExecuteDrawColorAreaROIAsync()
        {
            var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive);
            var imageEdit = WpfTool.FindVisualChild<ImageEdit>(window);
            if (imageEdit?.ImageSource == null)
            {
                SendInfoDialog("请先获取图片！");
                return;
            }

            try
            {
                var roi = await imageEdit.DrawROIAsync("颜色面积");
                if (roi == null) return;

                if (SelectVisionFlow == null) return;

                SelectVisionFlow.DoubleParams["ROILeft"] = roi.Left;
                SelectVisionFlow.DoubleParams["ROITop"] = roi.Top;
                SelectVisionFlow.DoubleParams["ROIWidth"] = roi.Width;
                SelectVisionFlow.DoubleParams["ROIHeight"] = roi.Height;

                // 画矩形边框
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = roi.Width,
                    Height = roi.Height,
                    Stroke = Brushes.Lime,
                    StrokeThickness = 2,
                    StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 },
                    Tag = "颜色面积",
                };
                Canvas.SetLeft(rect, roi.Left);
                Canvas.SetTop(rect, roi.Top);
                imageEdit.Draw(rect);

                SendInfoDialog("颜色面积 ROI 已保存");
            }
            catch (Exception ex)
            {
                SendInfoDialog($"ROI 绘制失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 在 ImageEdit 上画矩形边框 + 中心十字
        /// </summary>
        private static void DrawRectVisual(ImageEdit imageEdit, double left, double top, double width, double height)
        {
            var rectBorder = new System.Windows.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Stroke = Brushes.Magenta,
                StrokeThickness = 2,
                Tag = "ORB匹配",
            };
            Canvas.SetLeft(rectBorder, left);
            Canvas.SetTop(rectBorder, top);
            imageEdit.Draw(rectBorder);

            double cx = left + width / 2.0, cy = top + height / 2.0;
            DrawCross(imageEdit, cx, cy);
        }

        /// <summary>
        /// 在 ImageEdit 上画圆形边框 + 中心十字
        /// </summary>
        private static void DrawCircleVisual(ImageEdit imageEdit, double cx, double cy, double radius)
        {
            var ellipse = new System.Windows.Shapes.Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = Brushes.Magenta,
                StrokeThickness = 2,
                Tag = "ORB匹配",
            };
            Canvas.SetLeft(ellipse, cx - radius);
            Canvas.SetTop(ellipse, cy - radius);
            imageEdit.Draw(ellipse);

            DrawCross(imageEdit, cx, cy);
        }

        /// <summary>
        /// 在 ImageEdit 上画中心十字
        /// </summary>
        private static void DrawCross(ImageEdit imageEdit, double cx, double cy)
        {
            const int len = 10;
            var crossH = new System.Windows.Shapes.Line
            {
                X1 = cx - len,
                Y1 = cy,
                X2 = cx + len,
                Y2 = cy,
                Stroke = Brushes.Magenta,
                StrokeThickness = 1,
                Tag = "ORB匹配",
            };
            imageEdit.Draw(crossH);
            var crossV = new System.Windows.Shapes.Line
            {
                X1 = cx,
                Y1 = cy - len,
                X2 = cx,
                Y2 = cy + len,
                Stroke = Brushes.Magenta,
                StrokeThickness = 1,
                Tag = "ORB匹配",
            };
            imageEdit.Draw(crossV);
        }

#endregion



    }

    /// <summary>
    /// 添加流程菜单中的一个分组（一个子菜单）
    /// </summary>
    public class FlowMenuGroup
    {
        /// <summary>
        /// Header
        /// </summary>
        public string Header { get; set; }
        /// <summary>
        /// Items
        /// </summary>
        public List<FlowMenuItem> Items { get; set; }
    }

    /// <summary>
    /// 添加流程菜单中的一个叶子菜单项
    /// </summary>
    public class FlowMenuItem
    {
        /// <summary>
        /// Header
        /// </summary>
        public string Header { get; set; }
        /// <summary>
        /// Flow类型
        /// </summary>
        public VisionFlowType FlowType { get; set; }
    }

}
