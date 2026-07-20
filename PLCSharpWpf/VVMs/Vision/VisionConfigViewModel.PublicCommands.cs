using OpenCvSharp;
using PLCSharp.Core.UserControls;
using PLCSharp.VVMs.GlobalVariables;
using PLCSharp.VVMs.Vision.VisionFlowHandler;
using Prism.Commands;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PLCSharp.VVMs.Vision;

/// <summary>
/// 视觉流程配置界面 —— 公共指令（ROI 绘制、模板计算、标定等）
/// </summary>
public partial class VisionConfigViewModel
{
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
        var imageEdit = SelectedVisionFunction?.EditImageEdit;
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
                    Points =
                    [
                        new System.Windows.Point(roi.EndX, roi.EndY),
                        new System.Windows.Point(roi.EndX - headSize * (aux * 0.7 - auy * 0.7),
                                   roi.EndY - headSize * (auy * 0.7 + aux * 0.7)),
                        new System.Windows.Point(roi.EndX - headSize * (aux * 0.7 + auy * 0.7),
                                   roi.EndY - headSize * (auy * 0.7 - aux * 0.7)),
                    ],
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
        await ComputeTemplateAsync();
    }

    private AsyncDelegateCommand _DrawFindCircleROI;
    /// <summary>
    /// 绘制找圆ROI
    /// </summary>
    public AsyncDelegateCommand DrawFindCircleROI =>
        _DrawFindCircleROI ??= new AsyncDelegateCommand(ExecuteDrawFindCircleROIAsync);

    private async Task ExecuteDrawFindCircleROIAsync()
    {
        var imageEdit = SelectedVisionFunction?.EditImageEdit;
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
                StrokeDashArray = [3, 3],
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

        var imageEdit = SelectedVisionFunction?.EditImageEdit;
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
        var imageEdit = SelectedVisionFunction?.EditImageEdit;
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
            string matName = SelectVisionFlow.StringParams.TryGetValue("TransformMat", out var mn) && !string.IsNullOrEmpty(mn) ? mn : "标定矩阵";

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

            using Mat noInliers = new();
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
        await ComputeTemplateAsync();
    }

    private AsyncDelegateCommand _ComputeCircleTemplate;
    public AsyncDelegateCommand ComputeCircleTemplate =>
        _ComputeCircleTemplate ??= new AsyncDelegateCommand(ExecuteComputeCircleTemplateAsync);

    private async Task ExecuteComputeCircleTemplateAsync()
    {
        await ComputeTemplateAsync();
    }

    private AsyncDelegateCommand _ComputeRectTemplate;
    /// <summary>
    /// 计算矩形模板位置
    /// </summary>
    public AsyncDelegateCommand ComputeRectTemplate =>
        _ComputeRectTemplate ??= new AsyncDelegateCommand(ExecuteComputeRectTemplateAsync);

    private async Task ExecuteComputeRectTemplateAsync()
    {
        await ComputeTemplateAsync();
    }

    /// <summary>
    /// 通用模板计算：设标记 → RunItem → 弹提示 → 清标记
    /// </summary>
    private async Task ComputeTemplateAsync()
    {
        if (SelectVisionFlow == null || SelectedVisionFunction == null) return;
        SelectVisionFlow.StringParams["ComputeTemplate"] = "1";
        await Task.Run(() =>
        {
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
        });
    }

    private AsyncDelegateCommand _DrawORBTemplateROI;
    /// <summary>
    /// 绘制ORB模板ROI
    /// </summary>
    public AsyncDelegateCommand DrawORBTemplateROI =>
        _DrawORBTemplateROI ??= new AsyncDelegateCommand(ExecuteDrawORBTemplateROIAsync);

    private async Task ExecuteDrawORBTemplateROIAsync()
    {
        var imageEdit = SelectedVisionFunction?.EditImageEdit;
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
                        imageEdit.DrawRectVisual(roi.Left, roi.Top, roi.Width, roi.Height);
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
                        imageEdit.DrawCircleVisual(roi.CenterX, roi.CenterY, roi.Radius);
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
                              && !string.IsNullOrEmpty(tn) ? tn : "Template";

        var existing = SelectedVisionFunction.LocalImageDatas
            .FirstOrDefault(m => m.Name == templateName);
        if (existing != null)
        {
            existing.Mat?.Dispose();

            existing.Mat = templateMat;
        }
        else
        {

            SelectedVisionFunction.LocalImageDatas.Add(new LocalImageData
            {
                FuncID = SelectedVisionFunction.ID,
                Name = templateName,
                Mat = templateMat
            });
        }

        var localImageData = VisionsModel.
                            _DatasContext.
                            LocalImageDatas.
                            FirstOrDefault(m => m.Name == templateName
                                            && m.FuncID == SelectedVisionFunction.ID);
        if (localImageData != null)
        {
            localImageData.Mat = templateMat;

        }
        else
        {

            VisionsModel._DatasContext.Add(new LocalImageData
            {
                FuncID = SelectedVisionFunction.ID,
                Name = templateName,
                Mat = templateMat
            });
        }
        VisionsModel._DatasContext.Save();
    }

    private AsyncDelegateCommand _DrawGrayTemplateMatchROI;
    /// <summary>
    /// 绘制灰度模板匹配模板 ROI
    /// </summary>
    public AsyncDelegateCommand DrawGrayTemplateMatchROI =>
        _DrawGrayTemplateMatchROI ??= new AsyncDelegateCommand(ExecuteDrawGrayTemplateMatchROIAsync);

    private async Task ExecuteDrawGrayTemplateMatchROIAsync()
    {
        var imageEdit = SelectedVisionFunction?.EditImageEdit;
        if (imageEdit?.ImageSource == null)
        {
            SendInfoDialog("请先获取图片！");
            return;
        }

        try
        {
            if (SelectVisionFlow == null) return;

            string roiShape = SelectVisionFlow.StringParams.TryGetValue("ROIShape", out var rs)
                && !string.IsNullOrEmpty(rs) ? rs : "矩形";

            double centerX = 0, centerY = 0;
            double templateW = 0, templateH = 0;

            switch (roiShape)
            {
                case "矩形":
                    {
                        var roi = await imageEdit.DrawROIAsync("灰度模板匹配");
                        if (roi == null) return;

                        SelectVisionFlow.DoubleParams["TemplateLeft"] = roi.Left;
                        SelectVisionFlow.DoubleParams["TemplateTop"] = roi.Top;
                        SelectVisionFlow.DoubleParams["TemplateWidth"] = roi.Width;
                        SelectVisionFlow.DoubleParams["TemplateHeight"] = roi.Height;

                        centerX = roi.Left + roi.Width / 2.0;
                        centerY = roi.Top + roi.Height / 2.0;
                        templateW = roi.Width;
                        templateH = roi.Height;

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

                        imageEdit.DrawRectVisual(roi.Left, roi.Top, roi.Width, roi.Height);
                        break;
                    }

                case "旋转矩形":
                    {
                        var roi = await imageEdit.DrawRotateRectROIAsync("灰度模板匹配");
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

                        var srcMat = SelectedVisionFunction.Src;
                        if (srcMat != null && !srcMat.Empty())
                        {
                            double cx = roi.CenterX, cy = roi.CenterY;
                            double w = roi.RectWidth, h = roi.RectHeight;
                            double rad = roi.RectAngle * Math.PI / 180.0;
                            double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
                            double hw = w / 2.0, hh = h / 2.0;
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
                                SelectVisionFlow.DoubleParams["TemplateBBoxLeft"] = bx;
                                SelectVisionFlow.DoubleParams["TemplateBBoxTop"] = by;
                                SelectVisionFlow.DoubleParams["TemplateBBoxWidth"] = bw;
                                SelectVisionFlow.DoubleParams["TemplateBBoxHeight"] = bh;

                                using var cropped = srcMat[new OpenCvSharp.Rect(bx, by, bw, bh)].Clone();
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
                        break;
                    }

                case "圆形":
                    {
                        var roi = await imageEdit.DrawCircleAsync("灰度模板匹配");
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

                        var srcMat = SelectedVisionFunction.Src;
                        if (srcMat != null && !srcMat.Empty())
                        {
                            double r = roi.Radius;
                            int x = Math.Clamp((int)(roi.CenterX - r), 0, srcMat.Width - 1);
                            int y = Math.Clamp((int)(roi.CenterY - r), 0, srcMat.Height - 1);
                            int size = Math.Min((int)(r * 2), Math.Min(srcMat.Width - x, srcMat.Height - y));
                            if (size > 0)
                            {
                                using var cropped = srcMat[new OpenCvSharp.Rect(x, y, size, size)].Clone();
                                using var mask = new Mat(cropped.Size(), MatType.CV_8UC1, Scalar.Black);
                                Cv2.Circle(mask, new OpenCvSharp.Point(size / 2, size / 2), size / 2, Scalar.White, -1);
                                Mat result = Mat.Zeros(cropped.Size(), cropped.Type());
                                cropped.CopyTo(result, mask);
                                SaveTemplateMat(result);
                            }
                        }

                        imageEdit.DrawCircleVisual(roi.CenterX, roi.CenterY, roi.Radius);
                        break;
                    }
            }

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

    private AsyncDelegateCommand _DrawBarcodeROI;
    /// <summary>
    /// 绘制条码解码 ROI 区域
    /// </summary>
    public AsyncDelegateCommand DrawBarcodeROI =>
        _DrawBarcodeROI ??= new AsyncDelegateCommand(ExecuteDrawBarcodeROIAsync);

    private async Task ExecuteDrawBarcodeROIAsync()
    {
        await DrawRectROIAsync("条码解码");
    }

    private AsyncDelegateCommand _DrawColorAreaROI;
    /// <summary>
    /// 绘制颜色面积 ROI 区域
    /// </summary>
    public AsyncDelegateCommand DrawColorAreaROI =>
        _DrawColorAreaROI ??= new AsyncDelegateCommand(ExecuteDrawColorAreaROIAsync);

    private async Task ExecuteDrawColorAreaROIAsync()
    {
        await DrawRectROIAsync("颜色面积");
    }

    private AsyncDelegateCommand _DrawGrayAreaROI;
    /// <summary>
    /// 绘制灰度面积 ROI 区域
    /// </summary>
    public AsyncDelegateCommand DrawGrayAreaROI =>
        _DrawGrayAreaROI ??= new AsyncDelegateCommand(ExecuteDrawGrayAreaROIAsync);

    private async Task ExecuteDrawGrayAreaROIAsync()
    {
        await DrawRectROIAsync("灰度面积");
    }

    private AsyncDelegateCommand _DrawROICropROI;
    /// <summary>
    /// 绘制ROI剪切 ROI 区域
    /// </summary>
    public AsyncDelegateCommand DrawROICropROI =>
        _DrawROICropROI ??= new AsyncDelegateCommand(ExecuteDrawROICropROIAsync);

    private async Task ExecuteDrawROICropROIAsync()
    {
        await DrawRectROIAsync("ROI剪切");
    }

    /// <summary>
    /// 通用矩形 ROI 绘制：在活动窗口的 ImageEdit 上拖拽矩形 → 保存参数 → 绘制边框 → 弹窗提示
    /// </summary>
    private async Task DrawRectROIAsync(string roiName)
    {
        var imageEdit = SelectedVisionFunction?.EditImageEdit;
        if (imageEdit?.ImageSource == null)
        {
            SendInfoDialog("请先获取图片！");
            return;
        }

        try
        {
            var roi = await imageEdit.DrawROIAsync(roiName);
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
                StrokeDashArray = [4, 2],
                Tag = roiName,
            };
            Canvas.SetLeft(rect, roi.Left);
            Canvas.SetTop(rect, roi.Top);
            imageEdit.Draw(rect);

            SendInfoDialog($"{roiName} ROI 已保存");
        }
        catch (Exception ex)
        {
            SendInfoDialog($"ROI 绘制失败：{ex.Message}");
        }
    }

    #endregion
}
