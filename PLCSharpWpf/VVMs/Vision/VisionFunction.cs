
using Newtonsoft.Json;
using OpenCvSharp;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.Core.UserControls;
using PLCSharp.Models;
using PLCSharp.VVMs.Vision.VisionFlowHandler;
using PLCSharp.VVMs.Vision.VisionFlowHandler.Access;
using PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm;
using PLCSharp.VVMs.Vision.VisionFlowHandler.Processing;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Media;

namespace PLCSharp.VVMs.Vision
{

    /// <summary>
    /// 视觉功能的执行单元，包含流程步骤列表（VisionFlows）、参数集合（Params）和运行时图像缓存
    /// </summary>
    public class VisionFunction : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 配方标识
        /// </summary>
        public Guid RecipeID { get; set; }

        private string _Name;
        /// <summary>
        /// 配置项
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

        private string _Prompt;
        /// <summary>
        /// 提示
        /// </summary>
        [NotMapped]
        public string Prompt
        {
            get { return _Prompt; }
            set { SetProperty(ref _Prompt, value); }
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


        private string _ControlName;
        /// <summary>
        /// 关联的首页图像控件
        /// </summary>
        public string ControlName
        {
            get { return _ControlName; }
            set { SetProperty(ref _ControlName, value); }
        }
        #region 结果
        [NotMapped]
        public Pos ResultPos { get; set; } = new();

        [NotMapped]
        public Rect ResultRect { get; set; } = new();

        [NotMapped]
        public Circle ResultCircle { get; set; } = new();

        [NotMapped]
        public Line ResultLine { get; set; } = new();

        [NotMapped]
        public Barcode ResultBarcode { get; set; } = new();

        [NotMapped]
        public double ResultDouble { get; set; }

        [NotMapped]
        public string ResultString { get; set; }

        [NotMapped]
        public List<Pos> ResultPosList { get; set; } = [];

        [NotMapped]
        public List<Rect> ResultRectList { get; set; } = [];

        [NotMapped]
        public List<Circle> ResultCircleList { get; set; } = [];

        [NotMapped]
        public List<Line> ResultLineList { get; set; } = [];

        [NotMapped]
        public List<Barcode> ResultBarcodeList { get; set; } = [];

        [NotMapped]
        public List<double> ResultDoubleList { get; set; } = [];

        [NotMapped]
        public List<string> ResultStringList { get; set; } = [];
        #endregion


        /// <summary>
        /// 编辑时显示的控件
        /// </summary>
        [NotMapped]


        public ImageEdit EditImageEdit { get; set; }

        private Mat _Src;
        /// <summary>
        /// 源图像
        /// </summary>
        [NotMapped]
        public Mat Src
        {
            get { return _Src; }
            set
            {

                var old = _Src;
                if (old != value)
                {
                    _Src = value;
                    old?.Dispose();    // 释放旧 Mat 的非托管资源
                }



            }
        }

        private VisionParams _Params = new();

        // VisionFunction 中添加辅助方法


        /// <summary>
        /// 参数集合
        /// </summary>
        [NotMapped]
        public VisionParams Params
        {
            get { return _Params; }
            set { SetProperty(ref _Params, value); }
        }

        /// <summary>
        /// 序列化后的参数 JSON
        /// </summary>
        [Column("VisionParams")]
        public string SerializedVisionParams
        {
            get => JsonConvert.SerializeObject(Params); // 自动序列化
            set
            {
                try
                {
                    Params = value != null ? JsonConvert.DeserializeObject<VisionParams>(value) : new VisionParams(); // 自动反序列化
                }
                catch (global::System.Exception)
                {
                    Params = new VisionParams();

                }
            }


        }
        private ObservableCollection<VisionFlow> _VisionFlows = [];
        /// <summary>
        /// 流程步骤列表
        /// </summary>
        [NotMapped]
        public ObservableCollection<VisionFlow> VisionFlows
        {
            get { return _VisionFlows; }
            set
            {
                if (value == null) return;

                SetProperty(ref _VisionFlows, value);
            }
        }

        /// <summary>
        /// 序列化后的流程 JSON
        /// </summary>
        [Column("VisionFlows")]
        public string SerializedVisionFlows
        {
            get => JsonConvert.SerializeObject(VisionFlows); // 自动序列化
            set => VisionFlows = JsonConvert.DeserializeObject<ObservableCollection<VisionFlow>>(value); // 自动反序列化

        }

        int index = 0;
        /// <summary>
        /// 流程状态模型
        /// </summary>
        [NotMapped]
        public FlowModel Flow { get; set; } = new();
        /// <summary>
        /// 运行All
        /// </summary>
        /// <param name="flow">流程状态模型</param>
        /// <returns>返回布尔值</returns>
        public bool RunAll(FlowModel flow)
        {
            switch (flow.Step)
            {
                case 0:
                    index = 0;
                    flow.Step++;

                    break;
                case 1:
                    if (VisionFlows.Count > index)
                    {

                        VisionFlows[index].Flow.Reset();
                        flow.Step++;
                    }
                    else
                    {

                        var customControl = GlobalModel.GetCustomControl(ControlName);
                        if (customControl != null && Src != null)
                        {
                            customControl.ShowMat(Src);

                        }

                        flow.Done = true;

                    }
                    break;
                case 2:
                    if (RunItem(VisionFlows[index]))
                    {
                        flow.Step--;
                        index++;

                    }
                    break;
            }
            return flow.Done;

        }
        /// <summary>
        /// 全局图像列表
        /// </summary>
        [NotMapped]
        public ObservableCollection<ImageData> ImageDatas { get; set; }

        /// <summary>
        /// 视觉模型
        /// </summary>
        [NotMapped]
        public VisionsModel VisionsModel { get; set; }
        /// <summary>
        /// 运行项
        /// </summary>
        /// <param name="item">变量项</param>
        /// <returns>返回布尔值</returns>
        public bool RunItem(VisionFlow item)
        {
            if (_handlers.TryGetValue(item.Type, out var handler))
                return handler.Execute(this, item);
            return false;

        }


        private static readonly Dictionary<VisionFlowType, IVisionFlowHandler> _handlers = new()
        {
            [VisionFlowType.阈值] = new ThresholdHandler(),
            [VisionFlowType.GRAY2BGR] = new Gray2BgrHandler(),
            [VisionFlowType.BGR2GRAY] = new Bgr2GrayHandler(),
            [VisionFlowType.取通道] = new SplitChannelHandler(),

            [VisionFlowType.各通道最小值] = new MinChannelHandler(),
            [VisionFlowType.各通道最大值] = new MaxChannelHandler(),
            [VisionFlowType.显示图像到主页] = new ShowImageHandler(),

            [VisionFlowType.从文件获取图片] = new GetFromFileHandler(),

            [VisionFlowType.存到文件] = new SaveImageToFileHandler(),
            [VisionFlowType.从全局图像获取图片] = new GetFromGlobalImageHandler(),
            [VisionFlowType.存到全局图像] = new SaveImageToGlobalHandler(),
            [VisionFlowType.从局部图像获取图片] = new GetImageFromProcessHandler(),
            [VisionFlowType.存到局部图像] = new SaveImageToProcessHandler(),
            [VisionFlowType.拍照] = new GetFromCameraHandler(),
            [VisionFlowType.卡尺寻边] = new CaliperFindEdgeHandler(),
            [VisionFlowType.ORB匹配] = new ORBMatchHandler(),
            [VisionFlowType.卡尺找圆] = new CaliperFindCircleHandler(),
            [VisionFlowType.卡尺找旋转矩形] = new CaliperFindRectHandler(),
            [VisionFlowType.清除绘制] = new ClearDrawHandler(),
            [VisionFlowType.两线交点] = new TwoLineIntersectHandler(),
            [VisionFlowType.坐标转换] = new CoordinateTransformHandler(),
            [VisionFlowType.ROI解码] = new BarcodeDecodeHandler(),
            [VisionFlowType.微信解码] = new WeChatDecodeHandler(),
            [VisionFlowType.颜色面积] = new ColorAreaHandler(),
            [VisionFlowType.灰度面积] = new GrayAreaHandler(),
            [VisionFlowType.图像翻转] = new ImageFlipHandler(),
            [VisionFlowType.图像旋转] = new ImageRotateHandler(),
            [VisionFlowType.ROI剪切] = new ROICropHandler(),

        };
        [NotMapped]
        public GlobalModel GlobalModel { get; set; }


        [NotMapped]
        public List<DrawCommand> DrawCommands { get; set; } = [];
        public async Task RenderDrawAsync()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
             {
                 if (DrawCommands == null || DrawCommands.Count <= 0) return;
                 var commands = DrawCommands.ToList();

                 if (EditImageEdit != null)
                 //编辑窗口显示
                 {


                     foreach (var cmd in commands)
                     {
                         if (cmd.IsDrawnEdit) continue; // 如果已经绘制过，则跳过

                         switch (cmd.Shape)
                         {
                             case DrawCommand.Type.Line:
                                 var line = new System.Windows.Shapes.Line
                                 {
                                     X1 = cmd.X1,
                                     Y1 = cmd.Y1,
                                     X2 = cmd.X2,
                                     Y2 = cmd.Y2,
                                     Stroke = new SolidColorBrush(cmd.Color),
                                     StrokeThickness = cmd.Thickness,
                                     Tag = "DrawOverlay"
                                 };
                                 EditImageEdit.Draw(line);
                                 break;
                             case DrawCommand.Type.Circle:
                                 if (cmd.Filled)
                                 {
                                     var dot = new System.Windows.Shapes.Ellipse
                                     {
                                         Width = cmd.Radius * 2,
                                         Height = cmd.Radius * 2,
                                         Fill = new SolidColorBrush(cmd.Color),
                                         Tag = "DrawOverlay"
                                     };
                                     EditImageEdit.Draw(dot, (int)(cmd.Y1 - cmd.Radius), (int)(cmd.X1 - cmd.Radius));
                                 }
                                 else
                                 {
                                     var circle = new System.Windows.Shapes.Ellipse
                                     {
                                         Width = cmd.Radius * 2,
                                         Height = cmd.Radius * 2,
                                         Stroke = new SolidColorBrush(cmd.Color),
                                         StrokeThickness = cmd.Thickness,
                                         Tag = "DrawOverlay"
                                     };
                                     EditImageEdit.Draw(circle, (int)(cmd.Y1 - cmd.Radius), (int)(cmd.X1 - cmd.Radius));
                                 }
                                 break;
                             case DrawCommand.Type.Text:
                                 var editText = new System.Windows.Controls.TextBlock
                                 {
                                     Text = cmd.Text,
                                     FontSize = cmd.FontSize,
                                     Foreground = new SolidColorBrush(cmd.Color),
                                     Tag = "DrawOverlay"
                                 };
                                 EditImageEdit.Draw(editText, (int)cmd.Y1, (int)cmd.X1);
                                 break;
                             case DrawCommand.Type.Polygon:
                                 if (cmd.Points != null && cmd.Points.Length >= 2)
                                 {
                                     var poly = new System.Windows.Shapes.Polygon
                                     {
                                         Points = new System.Windows.Media.PointCollection(
                                             cmd.Points.Select(p => new System.Windows.Point(p.X, p.Y))),
                                         Stroke = new SolidColorBrush(cmd.Color),
                                         StrokeThickness = cmd.Thickness,
                                         Fill = Brushes.Transparent,
                                         Tag = "DrawOverlay"
                                     };
                                     EditImageEdit.Draw(poly);
                                 }
                                 break;
                         }
                         cmd.IsDrawnEdit = true;
                     }
                 }
                 var imageEdit = GlobalModel.GetImageControl(ControlName);
                 if (imageEdit == null) return;

                 {
                     foreach (var cmd in commands)
                     {
                         if (cmd.IsDrawn) continue; // 如果已经绘制过，则跳过

                         switch (cmd.Shape)
                         {
                             case DrawCommand.Type.Line:
                                 var line = new System.Windows.Shapes.Line
                                 {
                                     X1 = cmd.X1,
                                     Y1 = cmd.Y1,
                                     X2 = cmd.X2,
                                     Y2 = cmd.Y2,
                                     Stroke = new SolidColorBrush(cmd.Color),
                                     StrokeThickness = cmd.Thickness,
                                     Tag = "DrawOverlay"
                                 };
                                 imageEdit.Draw(line);
                                 break;
                             case DrawCommand.Type.Circle:
                                 if (cmd.Filled)
                                 {
                                     var dot = new System.Windows.Shapes.Ellipse
                                     {
                                         Width = cmd.Radius * 2,
                                         Height = cmd.Radius * 2,
                                         Fill = new SolidColorBrush(cmd.Color),
                                         Tag = "DrawOverlay"
                                     };
                                     imageEdit.Draw(dot, (int)(cmd.Y1 - cmd.Radius), (int)(cmd.X1 - cmd.Radius));
                                 }
                                 else
                                 {
                                     var circle = new System.Windows.Shapes.Ellipse
                                     {
                                         Width = cmd.Radius * 2,
                                         Height = cmd.Radius * 2,
                                         Stroke = new SolidColorBrush(cmd.Color),
                                         StrokeThickness = cmd.Thickness,
                                         Tag = "DrawOverlay"
                                     };
                                     imageEdit.Draw(circle, (int)(cmd.Y1 - cmd.Radius), (int)(cmd.X1 - cmd.Radius));
                                 }
                                 break;
                             case DrawCommand.Type.Polygon:
                                 if (cmd.Points != null && cmd.Points.Length >= 2)
                                 {
                                     var poly = new System.Windows.Shapes.Polygon
                                     {
                                         Points = new System.Windows.Media.PointCollection(
                                             cmd.Points.Select(p => new System.Windows.Point(p.X, p.Y))),
                                         Stroke = new SolidColorBrush(cmd.Color),
                                         StrokeThickness = cmd.Thickness,
                                         Fill = Brushes.Transparent,
                                         Tag = "DrawOverlay"
                                     };
                                     imageEdit.Draw(poly);
                                 }
                                 break;
                             case DrawCommand.Type.Text:
                                 var mainText = new System.Windows.Controls.TextBlock
                                 {
                                     Text = cmd.Text,
                                     FontSize = cmd.FontSize,
                                     Foreground = new SolidColorBrush(cmd.Color),
                                     Tag = "DrawOverlay"
                                 };
                                 imageEdit.Draw(mainText, (int)cmd.Y1, (int)cmd.X1);
                                 break;
                         }
                         cmd.IsDrawn = true;
                     }

                 }
             });
        }
    }
    /// <summary>
    /// VisionParams
    /// </summary>
    public class VisionParams : BindableBase
    {

        private ObservableCollection<ImageData> _Mats = [];
        /// <summary>
        /// 图像集合
        /// </summary>
        public ObservableCollection<ImageData> ImageDatas
        {
            get { return _Mats; }
            set { SetProperty(ref _Mats, value); }
        }

        private ObservableDictionary<string, double> _ResultDoubles = [];
        /// <summary>
        /// 通用数值结果（如 ORB 匹配输出的 X/Y/角度偏移）
        /// </summary>
        public ObservableDictionary<string, double> ResultDoubles
        {
            get { return _ResultDoubles; }
            set { SetProperty(ref _ResultDoubles, value); }
        }

        private ObservableCollection<LocalVariableItem> _Variables = [];
        /// <summary>
        /// 局部变量表（坐标点、直线、圆、矩形、数值），序列化保存在 JSON 中
        /// </summary>
        public ObservableCollection<LocalVariableItem> Variables
        {
            get { return _Variables; }
            set { SetProperty(ref _Variables, value); }
        }


    }


}

