#nullable enable
using OpenCvSharp;
using System.Windows.Media;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    /// <summary>
    /// 微信解码 Handler — 封装 OpenCV WeChatQRCode（CNN 检测 + 解码 QR 码）
    /// 支持同时检测和解码图像中的多个 QR 码。
    /// </summary>
    public class WeChatDecodeHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.微信解码;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            if (func.Src == null || func.Src.Empty())
                throw new Exception("源图像为空！");

            // 1. 读取参数
            bool enableMirror = item.BoolParams.TryGetValue("EnableMirror", out bool em) && em;

            // 2. 获取源图像（BGR 或灰度皆可）
            using Mat src = func.Src.Clone();

            // 水平镜像（可选）
            if (enableMirror)
            {
                Cv2.Flip(src, src, FlipMode.X);
            }

            // 3. 创建 WeChatQRCode（无需外部模型，OpenCV 5.x 内嵌了检测器）
            using var qr = new WeChatQRCode("", "");

            // 4. 检测 + 解码（原生支持多个码）
            Point2f[][] corners;
            string[] texts;

            try
            {
                texts = qr.DetectAndDecode(src, out corners);
            }
            catch (Exception ex)
            {
                throw new Exception($"微信解码失败：{ex.Message}");
            }

            int count = texts.Length;

            // 5. 输出结果
            if (count > 0)
            {
                // 汇总所有解码文本
                string joinedText = string.Join(" | ", texts);
                item.StringParams["DecodeResult"] = joinedText;
                func.Params.ResultDoubles["DecodeSuccess"] = 1;
                func.Params.ResultDoubles["DecodeCount"] = count;

                // 对每个检测到的码：绘制检测框 + 文本标注
                for (int i = 0; i < count; i++)
                {
                    var pts = corners.Length > i ? corners[i] : [];
                    string text = texts[i];

                    if (pts.Length == 4)
                    {
                        // 绘制四边形边框
                        var polyPts = pts.Select(p => new System.Windows.Point(p.X, p.Y)).ToArray();
                        func.DrawCommands.Add(DrawCommand.Polygon(polyPts, Colors.Lime, 2));

                        // 文本标注在左上角（偏移行数避免重叠）
                        double labelX = pts[0].X;
                        double labelY = pts[0].Y - 10 - i * 16;
                        if (labelY < 0) labelY = pts[0].Y + 10 + i * 16;

                        string label = count > 1 ? $"[{i + 1}] " : "";
                        label += text.Length > 50 ? text[..50] + "…" : text;
                        func.DrawCommands.Add(new DrawCommand
                        {
                            Shape = DrawCommand.Type.Text,
                            X1 = labelX,
                            Y1 = labelY,
                            Text = label,
                            Color = Colors.Lime,
                            FontSize = 14,
                        });
                    }
                    else
                    {
                        // 无角点信息，在左上角依次排列
                        func.DrawCommands.Add(new DrawCommand
                        {
                            Shape = DrawCommand.Type.Text,
                            X1 = 10,
                            Y1 = 30 + i * 20,
                            Text = $"✓ [{i + 1}] " + (text.Length > 40 ? text[..40] + "…" : text),
                            Color = Colors.Lime,
                            FontSize = 16,
                        });
                    }
                }

                // 局部变量表 — 每个码存一个变量，变量名+序号递增
                string baseName = item.StringParams.TryGetValue("ResultVarName", out var rvn) && !string.IsNullOrEmpty(rvn)
                    ? rvn : "微信解码_Result";

                for (int i = 0; i < count; i++)
                {
                    var pts = corners.Length > i ? corners[i] : [];
                    string text = texts[i];

                    double boxCX = pts.Length >= 4
                        ? (pts[0].X + pts[1].X + pts[2].X + pts[3].X) / 4.0
                        : 0;
                    double boxCY = pts.Length >= 4
                        ? (pts[0].Y + pts[1].Y + pts[2].Y + pts[3].Y) / 4.0
                        : 0;

                    string varName = count > 1 ? $"{baseName}_{i + 1}" : baseName;
                    var existingVar = func.Params.Variables.FirstOrDefault(v => v.Name == varName);
                    if (existingVar == null)
                    {
                        existingVar = new LocalVariableItem(varName, "Barcode", new Barcode(new Pos(), ""));
                        System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(existingVar));
                    }
                    existingVar.RawValue = new Barcode(new Pos(boxCX, boxCY, 0, 0), text);
                }

                _ = func.RenderDrawAsync();

                item.Flow.Done = true;
                return true;
            }

            // 未找到 QR 码
            item.StringParams["DecodeResult"] = "";
            func.Params.ResultDoubles["DecodeSuccess"] = 0;
            func.Params.ResultDoubles["DecodeCount"] = 0;
            item.Flow.Done = true;
            return true;
        }
    }
}
