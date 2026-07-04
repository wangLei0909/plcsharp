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
        /// <summary>
        /// 提示
        /// </summary>
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

        [NotMapped]
        /// <summary>
        /// 编辑时显示的控件
        /// </summary>

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

        };
        [NotMapped]
        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }


        [NotMapped]
        /// <summary>
        /// 绘制命令列表，用于在图像上绘制线条、圆形等图形
        /// </summary>
        public List<DrawCommand> DrawCommands { get; set; } = [];
        public async Task RenderDrawAsync()
        {
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
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
        public ObservableCollection<ImageData> Mats
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

