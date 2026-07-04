using OpenCvSharp;
using System.Windows.Media;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    public class CaliperFindEdgeHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.卡尺寻边;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src ?? throw new Exception("请先获取图片！");
            if (!item.DoubleParams.TryGetValue("LineStartX", out double sx) ||
                !item.DoubleParams.TryGetValue("LineStartY", out double sy) ||
                !item.DoubleParams.TryGetValue("LineEndX", out double ex) ||
                !item.DoubleParams.TryGetValue("LineEndY", out double ey))
                throw new Exception("卡尺线未配置，请先画线！");

            var threshold = item.IntParams.TryGetValue("Threshold", out int t) ? Math.Max(t, 1) : 30;
            var numCalipers = item.IntParams.TryGetValue("NumCalipers", out int nc) ? Math.Max(nc, 1) : 50;
            var caliperLength = item.IntParams.TryGetValue("CaliperLength", out int cl) ? Math.Max(cl, 3) : 100;
            var direction = item.IntParams.TryGetValue("Direction", out int dir) ? dir : 0;
            var edgeSelector = item.IntParams.TryGetValue("EdgeSelector", out int es) ? Math.Clamp(es, 0, 1) : 0;
            var minScore = item.IntParams.TryGetValue("MinScore", out int ms) ? Math.Clamp(ms, 0, 100) : 10;
            var inlierTh = item.DoubleParams.TryGetValue("RansacTh", out double ith) ? Math.Max(ith, 0.1) : 5;
            var edgeName = item.StringParams.TryGetValue("EdgeName", out var en) && !string.IsNullOrEmpty(en)
                ? en : "卡尺寻边_Edge";

            if (src.Channels() == 3)
                Cv2.CvtColor(src, src, ColorConversionCodes.BGR2GRAY);

            double lineDx = ex - sx, lineDy = ey - sy;
            double lineLen = Math.Sqrt(lineDx * lineDx + lineDy * lineDy);
            if (lineLen < 1) throw new Exception("直线太短！");
            double ux = lineDx / lineLen, uy = lineDy / lineLen;
            double pdx = -uy, pdy = ux;
            double halfLen = caliperLength / 2.0;
            int scanLen = caliperLength;

            var edgePoints = new List<Point2d>();

            for (int k = 0; k < numCalipers; k++)
            {
                double frac = (k + 0.5) / numCalipers;
                double cx = sx + frac * lineDx, cy = sy + frac * lineDy;

                float[] profile = new float[scanLen];
                for (int j = 0; j < scanLen; j++)
                {
                    double pos = j - halfLen + 0.5;
                    double px = cx + pos * pdx, py = cy + pos * pdy;
                    int x0 = (int)Math.Floor(px), y0 = (int)Math.Floor(py);
                    int x1 = x0 + 1, y1 = y0 + 1;
                    double fx = px - x0, fy = py - y0;
                    x0 = Math.Clamp(x0, 0, src.Width - 1); x1 = Math.Clamp(x1, 0, src.Width - 1);
                    y0 = Math.Clamp(y0, 0, src.Height - 1); y1 = Math.Clamp(y1, 0, src.Height - 1);
                    double v00 = src.At<byte>(y0, x0), v10 = src.At<byte>(y0, x1);
                    double v01 = src.At<byte>(y1, x0), v11 = src.At<byte>(y1, x1);
                    profile[j] = (float)((1 - fy) * ((1 - fx) * v00 + fx * v10) + fy * ((1 - fx) * v01 + fx * v11));
                }

                // 找边缘：第一个/最后一个
                float[] grad = new float[scanLen];
                int? selIdx = null; float selGrad = 0;
                for (int j = 1; j < scanLen - 1; j++)
                {
                    grad[j] = profile[j + 1] - profile[j - 1];
                    bool dirMatch = direction == 2 || direction == 0 && grad[j] > 0 || direction == 1 && grad[j] < 0;
                    if (dirMatch && Math.Abs(grad[j]) >= threshold)
                    {
                        if (edgeSelector == 0) { selIdx = j; selGrad = grad[j]; break; }
                        else { selIdx = j; selGrad = grad[j]; }
                    }
                }

                if (selIdx.HasValue && Math.Abs(selGrad) / 2.55f >= minScore)
                {
                    float e0 = grad[Math.Max(selIdx.Value - 1, 0)];
                    float e2 = grad[Math.Min(selIdx.Value + 1, scanLen - 1)];
                    float denom = e0 - 2 * selGrad + e2;
                    float sp = 0;
                    if (Math.Abs(denom) > 1e-6f) sp = Math.Clamp(0.5f * (e0 - e2) / denom, -1.0f, 1.0f);
                    float edgePos = Math.Clamp(selIdx.Value + sp, 0.5f, scanLen - 1.5f);
                    double offset = edgePos - halfLen + 0.5;
                    edgePoints.Add(new Point2d(cx + offset * pdx, cy + offset * pdy));
                }
            }

            if (edgePoints.Count < 2)
                throw new Exception($"仅找到 {edgePoints.Count} 个边缘点，至少需要2个！");

            // RANSAC 内点
            var inls = RansacInliers(edgePoints, inlierTh);

            // 可视化 — 写入 DrawCommands
      
            var cyan = Color.FromArgb(128, 0, 255, 255);
            var lime = Color.FromArgb(128, 0, 255, 0);
            var magenta = Color.FromArgb(128, 255, 0, 255);

            for (int k = 0; k < numCalipers; k++)
            {
                double frac = (k + 0.5) / numCalipers;
                double cx = sx + frac * lineDx, cy = sy + frac * lineDy;
                double x0 = cx - halfLen * pdx, y0 = cy - halfLen * pdy;
                double x1 = cx + halfLen * pdx, y1 = cy + halfLen * pdy;
                func.DrawCommands.Add(DrawCommand.Line(x0, y0, x1, y1, cyan));
                double adx = x1 - x0, ady = y1 - y0;
                double alen = Math.Sqrt(adx * adx + ady * ady);
                if (alen > 3)
                {
                    double au = adx / alen, av = ady / alen;
                    double hs = Math.Min(alen * 0.3, 8);
                    func.DrawCommands.Add(DrawCommand.Line(x1, y1, x1 - hs * (au * 0.7 - av * 0.7), y1 - hs * (av * 0.7 + au * 0.7), cyan));
                    func.DrawCommands.Add(DrawCommand.Line(x1, y1, x1 - hs * (au * 0.7 + av * 0.7), y1 - hs * (av * 0.7 - au * 0.7), cyan));
                }
            }

            foreach (var pt in edgePoints)
                func.DrawCommands.Add(DrawCommand.Circle(pt.X, pt.Y, 5, lime));

            double mx = inls.Average(p => p.X), my = inls.Average(p => p.Y);
            double vx = inls[^1].X - inls[0].X, vy = inls[^1].Y - inls[0].Y;
            double vl = Math.Sqrt(vx * vx + vy * vy);
            if (vl > 1)
            {
                double du = vx / vl, dv = vy / vl;
                double p0 = ((inls[0].X - mx) * du + (inls[0].Y - my) * dv);
                double pN = ((inls[^1].X - mx) * du + (inls[^1].Y - my) * dv);
                func.DrawCommands.Add(DrawCommand.Line(mx + p0 * du, my + p0 * dv, mx + pN * du, my + pN * dv, magenta, 2));
            }

            // 写入结果变量
            var variable = func.Params.Variables.FirstOrDefault(v => v.Name == edgeName);
            if (variable == null)
            {
                variable = new LocalVariableItem(edgeName, "Line", new Line(new Pos(), new Pos()));
                System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(variable));
            }
            variable.RawValue = new Line(
                new Pos(inls[0].X, inls[0].Y, 0, 0), new Pos(inls[^1].X, inls[^1].Y, 0, 0));

            // 模板模式：保存拟合线与首尾卡尺的交点
            if (item.StringParams.TryGetValue("ComputeTemplate", out var ct) && ct == "1")
            {
                // 拟合线 Line2D（首尾内点）
                var lineInls = new Line2D(inls[0].X, inls[0].Y, inls[^1].X, inls[^1].Y);

                // 第一条卡尺
                double fc = (0 + 0.5) / numCalipers;
                double fcx = sx + fc * lineDx, fcy = sy + fc * lineDy;
                var caliperFirst = new Line2D(fcx - halfLen * pdx, fcy - halfLen * pdy, fcx + halfLen * pdx, fcy + halfLen * pdy);
                var ts = Intersect(lineInls, caliperFirst);

                // 最后一条卡尺
                double lc = (numCalipers - 1 + 0.5) / numCalipers;
                double lcx = sx + lc * lineDx, lcy = sy + lc * lineDy;
                var caliperLast = new Line2D(lcx - halfLen * pdx, lcy - halfLen * pdy, lcx + halfLen * pdx, lcy + halfLen * pdy);
                var te = Intersect(lineInls, caliperLast);

                item.DoubleParams["TemplateLineStartX"] = Math.Round(ts.X, 3);
                item.DoubleParams["TemplateLineStartY"] = Math.Round(ts.Y, 3);
                item.DoubleParams["TemplateLineEndX"] = Math.Round(te.X, 3);
                item.DoubleParams["TemplateLineEndY"] = Math.Round(te.Y, 3);
                item.DoubleParams["TemplateLineAngle"] = Math.Round(Math.Atan2(te.Y - ts.Y, te.X - ts.X) * 180 / Math.PI, 3);
                item.Flow.Done = true;
                return true;
            }

            // 有模板时计算偏移
            if (item.DoubleParams.TryGetValue("TemplateLineStartX", out double tsx) &&
                item.DoubleParams.TryGetValue("TemplateLineStartY", out double tsy) &&
                item.DoubleParams.TryGetValue("TemplateLineEndX", out double tex) &&
                item.DoubleParams.TryGetValue("TemplateLineEndY", out double tey))
            {
                // 计算模板中心偏移
                double cenX = (sx + ex) / 2, cenY = (sy + ey) / 2;
                double tCenX = (tsx + tex) / 2, tCenY = (tsy + tey) / 2;
                double offsetX = Math.Round(cenX - tCenX, 3);
                double offsetY = Math.Round(cenY - tCenY, 3);

                // 角度：统一用拟合线与首尾卡尺的交点
                double tAngle = Math.Atan2(tey - tsy, tex - tsx);
                var lineInls = new Line2D(inls[0].X, inls[0].Y, inls[^1].X, inls[^1].Y);
                double fc = (0 + 0.5) / numCalipers;
                double fcx = sx + fc * lineDx, fcy = sy + fc * lineDy;
                var caliperFirst = new Line2D(fcx - halfLen * pdx, fcy - halfLen * pdy, fcx + halfLen * pdx, fcy + halfLen * pdy);
                var cs = Intersect(lineInls, caliperFirst);
                double lc = (numCalipers - 1 + 0.5) / numCalipers;
                double lcx = sx + lc * lineDx, lcy = sy + lc * lineDy;
                var caliperLast = new Line2D(lcx - halfLen * pdx, lcy - halfLen * pdy, lcx + halfLen * pdx, lcy + halfLen * pdy);
                var ce = Intersect(lineInls, caliperLast);
                double cAngle = Math.Atan2(ce.Y - cs.Y, ce.X - cs.X);
                double offsetAngle = Math.Round((cAngle - tAngle) * 180 / Math.PI, 3);
                func.Params.ResultDoubles["LineOffsetX"] = offsetX;
                func.Params.ResultDoubles["LineOffsetY"] = offsetY;
                func.Params.ResultDoubles["LineOffsetAngle"] = offsetAngle;

                var offVarName = item.StringParams.TryGetValue("LineOffsetVar", out var ov) && !string.IsNullOrEmpty(ov) ? ov : "找线_Offset";
                var offVar = func.Params.Variables.FirstOrDefault(v => v.Name == offVarName);
                if (offVar == null) { offVar = new LocalVariableItem(offVarName, "Pos", new Pos()); System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(offVar)); }
                offVar.RawValue = new Pos(offsetX, offsetY, 0, offsetAngle);
            }
            _ = func.RenderDrawAsync();
            item.Flow.Done = true;
            return true;
        }

        private static Point2d Intersect(Line2D l1, Line2D l2)
        {
            double k1 = (l1.Y1 - l1.Vy) / (l1.X1 - l1.Vx);
            double k2 = (l2.Y1 - l2.Vy) / (l2.X1 - l2.Vx);
            double ix = (k1 * l1.Vx - l1.Vy - k2 * l2.Vx + l2.Vy) / (k1 - k2);
            double iy = (k1 * k2 * (l1.Vx - l2.Vx) + k1 * l2.Vy - k2 * l1.Vy) / (k1 - k2);
            return new Point2d(ix, iy);
        }

        private static List<Point2d> RansacInliers(List<Point2d> points, double threshold)
        {
            int n = points.Count;
            if (n <= 2) return points;
            var rng = new Random(42);
            int bestCnt = 0; double bvx = 0, bvy = 0, bx = 0, by = 0;
            for (int iter = 0; iter < Math.Min(100, n); iter++)
            {
                int i1 = rng.Next(n), i2 = rng.Next(n);
                while (i2 == i1) i2 = rng.Next(n);
                double vx = points[i2].X - points[i1].X, vy = points[i2].Y - points[i1].Y;
                double len = Math.Sqrt(vx * vx + vy * vy);
                if (len < 1e-10) continue;
                vx /= len; vy /= len;
                int cnt = 0;
                for (int i = 0; i < n; i++)
                { double dx = points[i].X - points[i1].X, dy = points[i].Y - points[i1].Y;
                  if (Math.Abs(dx * vy - dy * vx) <= threshold) cnt++; }
                if (cnt > bestCnt) { bestCnt = cnt; bvx = vx; bvy = vy; bx = points[i1].X; by = points[i1].Y; }
            }
            var result = points.Where(p => Math.Abs((p.X - bx) * bvy - (p.Y - by) * bvx) <= threshold).ToList();
            result.Sort((a, b) => (a.X * bvx + a.Y * bvy).CompareTo(b.X * bvx + b.Y * bvy));
            return result;
        }
    }
}
