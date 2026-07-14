#nullable enable
using OpenCvSharp;
using PLCSharp.Core.Common;
using System.Runtime.InteropServices;
using System.Windows.Media;
using ZXing;
using ZXing.Common;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    /// <summary>
    /// 条码解码 Handler — 多管线增强版
    ///
    /// 管线策略（按优先级）：
    ///   0  原图灰度
    ///   1  CLAHE + Otsu
    ///   2  自适应阈值
    ///   3  形态学闭运算（3×3）
    ///   4  锐化 + Otsu
    ///   5  大核形态学闭运算（5×5，重度破损码）
    ///
    /// 增强措施（所有管线共用）：
    ///   • Upscale：ROI 边长 &lt; 200 时自动放大 2-4 倍，保证 ZXing 检测器有足够像素
    ///   • PureBarcode：勾选后且设了 ROI 时，绕过 WhiteRectangleDetector 直接采样
    ///   • 垂直翻转兜底（管线 0 额外尝试）
    /// </summary>
    public class BarcodeDecodeHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.ROI解码;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            if (func.Src == null || func.Src.Empty())
                throw new Exception("源图像为空！");

            // 1. 读取参数
            int decodeType = item.IntParams.TryGetValue("DecodeType", out int dt) ? dt : 0;
            bool enableMirror = item.BoolParams.TryGetValue("EnableMirror", out bool em) && em;
            bool usePureBarcode = item.BoolParams.TryGetValue("UsePureBarcode", out bool pb) && pb;
            bool enableUpscale = item.BoolParams.TryGetValue("EnableUpscale", out bool up) ? up : true;

            // 2. ROI 裁剪
            Mat roiMat;
            int roiLeft = 0, roiTop = 0;
            double left = 0, top = 0, width = 0, height = 0;
            bool hasRoi = item.DoubleParams.TryGetValue("ROILeft", out left) &&
                          item.DoubleParams.TryGetValue("ROITop", out top) &&
                          item.DoubleParams.TryGetValue("ROIWidth", out width) &&
                          item.DoubleParams.TryGetValue("ROIHeight", out height) &&
                          width > 0 && height > 0;

            if (hasRoi)
            {
                roiLeft = (int)left;
                roiTop = (int)top;
                int w = (int)width;
                int h = (int)height;
                int x = Math.Clamp(roiLeft, 0, func.Src.Width - 1);
                int y = Math.Clamp(roiTop, 0, func.Src.Height - 1);
                w = Math.Min(w, func.Src.Width - x);
                h = Math.Min(h, func.Src.Height - y);
                if (w < 4 || h < 4)
                    throw new Exception("ROI 区域太小，至少需要 4×4 像素！");
                roiMat = func.Src[new OpenCvSharp.Rect(x, y, w, h)].Clone();
            }
            else
            {
                roiMat = func.Src.Clone();
            }

            // 3. 镜像 ROI 区域（只镜像 ROI，不影响原图）
            if (enableMirror)
            {
                Cv2.Flip(roiMat, roiMat, FlipMode.X);
            }

            // 4. 转换为灰度
            using var grayBase = new Mat();
            if (roiMat.Channels() == 3)
                Cv2.CvtColor(roiMat, grayBase, ColorConversionCodes.BGR2GRAY);
            else if (roiMat.Channels() == 1)
                roiMat.CopyTo(grayBase);
            else
            {
                roiMat.Dispose();
                throw new Exception("不支持的图像通道数！");
            }
            roiMat.Dispose();

            int imgW = grayBase.Width;
            int imgH = grayBase.Height;
            var formats = DecodeTypeToFormats(decodeType);

            // 5. 自动放大 — 小码放大给检测器足够的像素
            using var workingGray = BuildWorkingGray(grayBase, enableUpscale, out double actualScale);
            int workW = workingGray.Width;
            int workH = workingGray.Height;

            // 记录实际 ROI 偏移 + 放大系数，供绘制时反向映射
            double scaleInv = 1.0 / actualScale;

            // 6. PureBarcode 模式：仅当 ROI 紧密框住码时生效，绕过最脆弱的 WhiteRectangleDetector
            if (usePureBarcode && hasRoi)
            {
                var pbResult = TryDecodePureBarcode(workingGray, formats);
                if (pbResult != null)
                {
                    return OutputResult(func, item, pbResult, roiLeft, roiTop, workW, workH, scaleInv, pipeline: -1);
                }
            }

            // 7. 依次尝试多条预处理管线
            Result? result = null;
            int usedPipeline = -1;

            for (int pipeline = 0; pipeline < 6 && result == null; pipeline++)
            {
                using var processed = PreparePipeline(workingGray, pipeline);
                if (processed == null || processed.Empty())
                    continue;

                result = TryDecodeWithReader(processed, formats);

                if (result != null)
                {
                    usedPipeline = pipeline;
                    break;
                }

                // 管线 0 额外尝试垂直翻转
                if (pipeline == 0)
                {
                    Cv2.Flip(processed, processed, FlipMode.Y);
                    result = TryDecodeWithReader(processed, formats);
                    if (result != null)
                    {
                        usedPipeline = pipeline;
                        break;
                    }
                }
            }

            // 8. 输出
            if (result != null)
            {
                return OutputResult(func, item, result, roiLeft, roiTop, workW, workH, scaleInv, usedPipeline);
            }

            // 解码失败
            item.StringParams["DecodeResult"] = "";
            item.IntParams["UsedPipeline"] = -1;
            func.Params.ResultDoubles["DecodeSuccess"] = 0;

            if (roiLeft > 0 || roiTop > 0 || imgW > 0)
            {
                func.DrawCommands.Add(DrawCommand.Line(roiLeft, roiTop, roiLeft + imgW, roiTop, Colors.Red, 1));
                func.DrawCommands.Add(DrawCommand.Line(roiLeft + imgW, roiTop, roiLeft + imgW, roiTop + imgH, Colors.Red, 1));
                func.DrawCommands.Add(DrawCommand.Line(roiLeft + imgW, roiTop + imgH, roiLeft, roiTop + imgH, Colors.Red, 1));
                func.DrawCommands.Add(DrawCommand.Line(roiLeft, roiTop + imgH, roiLeft, roiTop, Colors.Red, 1));
            }

            item.Flow.Done = true;
            return true;
        }

        // ── 辅助方法 ────────────────────────────────────────

        /// <summary>构造放大后的灰度图</summary>
        private static Mat BuildWorkingGray(Mat gray, bool enableUpscale, out double scale)
        {
            if (!enableUpscale)
            {
                scale = 1.0;
                return gray.Clone();
            }

            int minDim = Math.Min(gray.Width, gray.Height);
            if (minDim >= 200)
            {
                scale = 1.0;
                return gray.Clone();
            }

            // 目标：小的边至少 300px，但不超过 800px
            int targetDim = Math.Min(Math.Max(minDim * 2, 300), 800);
            scale = (double)targetDim / minDim;
            int newW = (int)(gray.Width * scale);
            int newH = (int)(gray.Height * scale);
            var enlarged = new Mat();
            Cv2.Resize(gray, enlarged, new Size(newW, newH), 0, 0, InterpolationFlags.Cubic);
            return enlarged;
        }

        /// <summary>尝试 PureBarcode 解码（跳过 WhiteRectangleDetector）</summary>
        private static Result? TryDecodePureBarcode(Mat gray, List<BarcodeFormat> formats)
        {
            byte[] pixels = new byte[gray.Width * gray.Height];
            Marshal.Copy(gray.Data, pixels, 0, pixels.Length);
            var source = new RGBLuminanceSource(pixels, gray.Width, gray.Height, RGBLuminanceSource.BitmapFormat.Gray8);

            var reader = new BarcodeReaderGeneric
            {
                Options = new DecodingOptions
                {
                    PossibleFormats = formats,
                    TryHarder = true,
                    TryInverted = true,
                    PureBarcode = true,
                }
            };
            return reader.Decode(source);
        }

        /// <summary>用标准 reader 解码</summary>
        private static Result? TryDecodeWithReader(Mat mat, List<BarcodeFormat> formats)
        {
            byte[] pixels = new byte[mat.Width * mat.Height];
            Marshal.Copy(mat.Data, pixels, 0, pixels.Length);
            var source = new RGBLuminanceSource(pixels, mat.Width, mat.Height, RGBLuminanceSource.BitmapFormat.Gray8);

            var reader = new BarcodeReaderGeneric
            {
                Options = new DecodingOptions
                {
                    PossibleFormats = formats,
                    TryHarder = true,
                    TryInverted = true,
                }
            };
            return reader.Decode(source);
        }

        /// <summary>写入输出结果和绘制命令</summary>
        private static bool OutputResult(
            VisionFunction func, VisionFlow item, Result result,
            int roiLeft, int roiTop, int workW, int workH, double scaleInv, int pipeline)
        {
            string decodedText = result.Text;
            item.StringParams["DecodeResult"] = decodedText;
            item.IntParams["UsedPipeline"] = pipeline;
            func.Params.ResultDoubles["DecodeSuccess"] = 1;

            // 绘制条码边框（结果点是在放大后图像上的坐标，需要缩回原始尺寸 + ROI 偏移）
            if (result.ResultPoints != null && result.ResultPoints.Length >= 2)
            {
                var pts = result.ResultPoints
                    .Select(p => new System.Windows.Point(p.X * scaleInv + roiLeft, p.Y * scaleInv + roiTop))
                    .ToArray();

                if (pts.Length == 4)
                {
                    func.DrawCommands.Add(DrawCommand.Polygon(pts, Colors.Lime, 2));
                }
                else if (pts.Length == 2)
                {
                    double x1 = Math.Min(pts[0].X, pts[1].X);
                    double y1 = Math.Min(pts[0].Y, pts[1].Y);
                    double x2 = Math.Max(pts[0].X, pts[1].X);
                    double y2 = Math.Max(pts[0].Y, pts[1].Y);
                    func.DrawCommands.Add(DrawCommand.Line(x1, y1, x2, y1, Colors.Lime, 2));
                    func.DrawCommands.Add(DrawCommand.Line(x2, y1, x2, y2, Colors.Lime, 2));
                    func.DrawCommands.Add(DrawCommand.Line(x2, y2, x1, y2, Colors.Lime, 2));
                    func.DrawCommands.Add(DrawCommand.Line(x1, y2, x1, y1, Colors.Lime, 2));
                }

                // 文本标注
                double tx = result.ResultPoints[0].X * scaleInv + roiLeft;
                double ty = result.ResultPoints[0].Y * scaleInv + roiTop - 10;
                if (ty < 0) ty = result.ResultPoints[0].Y * scaleInv + roiTop + 10;
                func.DrawCommands.Add(new DrawCommand
                {
                    Shape = DrawCommand.Type.Text,
                    X1 = tx,
                    Y1 = ty,
                    Text = decodedText.Length > 60 ? decodedText[..60] + "…" : decodedText,
                    Color = Colors.Lime,
                    FontSize = 14,
                });
            }
            else
            {
                func.DrawCommands.Add(new DrawCommand
                {
                    Shape = DrawCommand.Type.Text,
                    X1 = roiLeft + (int)(workW * scaleInv) / 2.0 - 50,
                    Y1 = roiTop + (int)(workH * scaleInv) / 2.0,
                    Text = "✓ " + (decodedText.Length > 40 ? decodedText[..40] + "…" : decodedText),
                    Color = Colors.Lime,
                    FontSize = 16,
                });
            }

            // 局部变量表 — 存储条码中心坐标
            double boxCX = 0, boxCY = 0;
            if (result.ResultPoints != null && result.ResultPoints.Length >= 2)
            {
                for (int i = 0; i < result.ResultPoints.Length; i++)
                {
                    boxCX += result.ResultPoints[i].X * scaleInv + roiLeft;
                    boxCY += result.ResultPoints[i].Y * scaleInv + roiTop;
                }
                boxCX /= result.ResultPoints.Length;
                boxCY /= result.ResultPoints.Length;
            }
            string varName = item.StringParams.TryGetValue("ResultVarName", out var rvn) && !string.IsNullOrEmpty(rvn)
                ? rvn : "条码解码_Result";
            var existingVar = func.Params.Variables.FirstOrDefault(v => v.Name == varName);
            if (existingVar == null)
            {
                existingVar = new LocalVariableItem(varName, "Barcode", new Barcode(new Pos(), ""));
                System.Windows.Application.Current.Dispatcher.Invoke(
                    () => func.Params.Variables.Add(existingVar));
            }
            existingVar.RawValue = new Barcode(new Pos(boxCX, boxCY, 0, 0), decodedText);

            item.Flow.Done = true;
            return true;
        }

        // ── 管线定义 ────────────────────────────────────────

        private static Mat? PreparePipeline(Mat gray, int pipeline)
        {
            return pipeline switch
            {
                0 => gray.Clone(),                                                   // 原始灰度
                1 => ApplyClaheOtsu(gray),                                            // CLAHE + Otsu
                2 => ApplyAdaptiveThreshold(gray),                                    // 自适应阈值
                3 => ApplyMorphologicalClose(gray, 3),                                // 闭运算 3×3
                4 => ApplySharpenedOtsu(gray),                                        // 锐化 + Otsu
                5 => ApplyMorphologicalClose(gray, 5),                                // 闭运算 5×5（重度破损）
                _ => null,
            };
        }

        private static Mat ApplyClaheOtsu(Mat gray)
        {
            var clahe = Cv2.CreateCLAHE(3.0, new Size(8, 8));
            var result = new Mat();
            clahe.Apply(gray, result);
            Cv2.Threshold(result, result, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
            return result;
        }

        private static Mat ApplyAdaptiveThreshold(Mat gray)
        {
            int bs = Math.Max(21, (int)(Math.Min(gray.Width, gray.Height) / 10) * 2 + 1);
            if (bs % 2 == 0) bs++;
            if (bs < 3) bs = 3;
            var result = new Mat();
            Cv2.AdaptiveThreshold(gray, result, 255,
                AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, bs, 10);
            return result;
        }

        private static Mat ApplyMorphologicalClose(Mat gray, int kernelSize)
        {
            var bin = new Mat();
            Cv2.Threshold(gray, bin, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(kernelSize, kernelSize));
            Cv2.MorphologyEx(bin, bin, MorphTypes.Close, kernel, iterations: 1);
            var dilateKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2));
            Cv2.Dilate(bin, bin, dilateKernel, iterations: 1);
            return bin;
        }

        private static Mat ApplySharpenedOtsu(Mat gray)
        {
            using var blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new Size(0, 0), 1.0);
            var sharp = new Mat();
            Cv2.AddWeighted(gray, 1.5, blurred, -0.5, 0, sharp);
            Cv2.Threshold(sharp, sharp, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
            return sharp;
        }

        private static List<BarcodeFormat> DecodeTypeToFormats(int decodeType)
        {
            return decodeType switch
            {
                0 => [BarcodeFormat.DATA_MATRIX],
                1 => [BarcodeFormat.QR_CODE],
                2 => [BarcodeFormat.DATA_MATRIX, BarcodeFormat.QR_CODE],
                3 => [BarcodeFormat.DATA_MATRIX, BarcodeFormat.QR_CODE, BarcodeFormat.AZTEC, BarcodeFormat.PDF_417],
                _ => [BarcodeFormat.DATA_MATRIX],
            };
        }
    }
}
