using PLCSharp.Core.Common;
using PLCSharp.VVMs.GlobalVariables;
using PLCSharp.VVMs.Vision.Camera;
using PLCSharp.VVMs.Vision.Models;
using PLCSharp.VVMs.Vision.VisionFlowHandler.Access;
using PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm;
using PLCSharp.VVMs.Vision.VisionFlowHandler.Processing;
using Prism.Commands;
using System.Reflection;
using System.Threading;

namespace PLCSharp.VVMs.Vision;

/// <summary>
/// 视觉流程配置界面 —— 流程步骤的增删、测试和参数编辑
/// </summary>
public partial class VisionConfigViewModel
{
    #region 视觉流程

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
                SelectedLocalImageData = null;
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
            case VisionFlowType.图像翻转:
                ContentRegion = new ImageFlip();
                break;
            case VisionFlowType.图像旋转:
                ContentRegion = new ImageRotate();
                break;
            case VisionFlowType.ROI剪切:
                ContentRegion = new ROICrop();
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
            case VisionFlowType.灰度面积:
                ContentRegion = new GrayArea();
                break;
            case VisionFlowType.灰度模板匹配:
                ContentRegion = new GrayTemplateMatch();
                break;
        }

        if (ContentRegion != null)
            ContentRegion.DataContext = this;
    }




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
    /// 测试选择流程
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
                case VisionFlowType.图像翻转:
                    newFlow.IntParams["FlipDirection"] = 0;
                    break;
                case VisionFlowType.图像旋转:
                    newFlow.DoubleParams["RotateAngle"] = 0;
                    newFlow.IntParams["ResizeMode"] = 0;
                    break;
                case VisionFlowType.ROI剪切:
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
                case VisionFlowType.灰度面积:
                    newFlow.IntParams["GrayMin"] = 0;
                    newFlow.IntParams["GrayMax"] = 255;
                    newFlow.DoubleParams["AreaMinPercent"] = 0;
                    newFlow.DoubleParams["AreaMaxPercent"] = 100;
                    newFlow.StringParams["ResultVarName"] = "灰度面积_Result";
                    break;
                case VisionFlowType.灰度模板匹配:
                    newFlow.DoubleParams["MinAngle"] = -10;
                    newFlow.DoubleParams["MaxAngle"] = 10;
                    newFlow.DoubleParams["AngleStep"] = 1;
                    newFlow.DoubleParams["MatchScoreMax"] = 0.9;
                    newFlow.StringParams["TemplateName"] = "GrayMatch_Template";
                    newFlow.StringParams["GrayTemplateMatch_PosVar"] = "灰度模板匹配_Pos";
                    break;
                default:
                    break;
            }

            SelectVisionFlow = null;
            SelectedGlobalImageData = null;

        }

    }

    #endregion

    #region 流程设置引用
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


    private ImageData _SelectedGlobalImageData;
    /// <summary>
    /// 在流程中选择的全局图像
    /// </summary>
    public ImageData SelectedGlobalImageData
    {
        get { return _SelectedGlobalImageData; }
        set
        {
            SetProperty(ref _SelectedGlobalImageData, value);
            if (value != null)

                SelectVisionFlow.StringParams["Image"] = value.Name;
        }
    }

    private ImageData _SelectedLocalImageData;
    /// <summary>
    /// 
    /// </summary>
    public ImageData SelectedLocalImageData
    {
        get { return _SelectedLocalImageData; }
        set
        {
            SetProperty(ref _SelectedLocalImageData, value);
            if (value != null)
                SelectVisionFlow.StringParams["Image"] = value.Name;
        }
    }
    #endregion
}
