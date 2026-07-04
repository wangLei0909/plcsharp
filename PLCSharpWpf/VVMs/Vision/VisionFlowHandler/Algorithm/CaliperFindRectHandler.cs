using OpenCvSharp;
using System.Linq;
using System.Windows.Media;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    /// <summary>
    /// 卡尺找旋转矩形Handler
    /// </summary>
    public class CaliperFindRectHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.卡尺找旋转矩形;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src ?? throw new Exception("请先获取图片！");
            if (!item.DoubleParams.TryGetValue("RectCenterX", out double cx) ||
                !item.DoubleParams.TryGetValue("RectCenterY", out double cy) ||
                !item.DoubleParams.TryGetValue("RectWidth", out double rw) ||
                !item.DoubleParams.TryGetValue("RectHeight", out double rh) ||
                !item.DoubleParams.TryGetValue("RectAngle", out double rectAngle))
                throw new Exception("旋转矩形ROI未配置，请先画旋转矩形！");

            var threshold = item.IntParams.TryGetValue("Threshold", out int t) ? Math.Max(t, 1) : 30;
            var numCalipers = item.IntParams.TryGetValue("NumCalipers", out int nc) ? Math.Max(nc, 4) : 50;
            var caliperLength = item.IntParams.TryGetValue("CaliperLength", out int cl) ? Math.Max(cl, 3) : 100;
            var direction = item.IntParams.TryGetValue("Direction", out int dir) ? dir : 0;
            var caliperDirection = item.IntParams.TryGetValue("CaliperDirection", out int cd) ? cd : 0;
            var edgeSelector = item.IntParams.TryGetValue("EdgeSelector", out int es) ? Math.Clamp(es, 0, 1) : 0;
            var minScore = item.IntParams.TryGetValue("MinScore", out int ms) ? Math.Clamp(ms, 0, 100) : 10;
            var minInliers = item.IntParams.TryGetValue("MinInliers", out int mi) ? Math.Max(mi, 3) : 10;
            var inlierTh = item.DoubleParams.TryGetValue("RansacTh", out double ith) ? Math.Max(ith, 0.1) : 5;
            var resultVar = item.StringParams.TryGetValue("RectResultVar", out var rv) && !string.IsNullOrEmpty(rv)
                ? rv : "卡尺找旋转矩形_Rect";

            if (src.Channels() == 3)
                Cv2.CvtColor(src, src, ColorConversionCodes.BGR2GRAY);

            double halfLen = caliperLength / 2.0;
            int scanLen = caliperLength;
            int calipersPerEdge = Math.Max(numCalipers / 4, 1);

            double rad = rectAngle * Math.PI / 180.0;
            double cosA = Math.Cos(rad), sinA = Math.Sin(rad);

            // 4 条边的方向（沿边）和扫描方向（垂直向外）
            // 边 0=右, 1=上, 2=左, 3=下
            (double edgeUx, double edgeUy, double scanDirX, double scanDirY)[] edges =
            [
                (-sinA,  cosA,  cosA,  sinA ), // 右
                ( cosA,  sinA, -sinA,  cosA ), // 上
                ( sinA, -cosA, -cosA, -sinA ), // 左
                (-cosA, -sinA,  sinA, -cosA ), // 下
            ];

            // 每条边的中点和长度
            double hw = rw / 2.0, hh = rh / 2.0;
            (double mx, double my, double len)[] edgeGeom =
            [
                (cx + hw * cosA, cy + hw * sinA, rh),                // 右
                (cx - hh * sinA, cy + hh * cosA, rw),                // 上
                (cx - hw * cosA, cy - hw * sinA, rh),                // 左
                (cx + hh * sinA, cy - hh * cosA, rw),                // 下
            ];

            var allEdgePoints = new List<List<Point2d>>();
            // 存储所有卡尺位置用于可视化
            var allCaliperLines = new List<(double sx0, double sy0, double sx1, double sy1, double dirX, double dirY)>();

            for (int ei = 0; ei < 4; ei++)
            {
                var (mx, my, len) = edgeGeom[ei];
                var (edgeUx, edgeUy, scanDirX, scanDirY) = edges[ei];
                double edgeLen = len;
                var edgePoints = new List<Point2d>();

                for (int k = 0; k < calipersPerEdge; k++)
                {
                    double frac = (k + 0.5) / calipersPerEdge;
                    double pos = (frac - 0.5) * edgeLen;
                    double scanCx = mx + pos * edgeUx;
                    double scanCy = my + pos * edgeUy;

                    // 扫描方向：从外向内时取反
                    double sdx = caliperDirection == 1 ? -scanDirX : scanDirX;
                    double sdy = caliperDirection == 1 ? -scanDirY : scanDirY;

                    // 记录卡尺线端点：扫描起点→终点
                    double clStartX = scanCx - halfLen * sdx;
                    double clStartY = scanCy - halfLen * sdy;
                    double clEndX = scanCx + halfLen * sdx;
                    double clEndY = scanCy + halfLen * sdy;
                    allCaliperLines.Add((clStartX, clStartY, clEndX, clEndY, sdx, sdy));

                    float[] profile = new float[scanLen];
                    for (int j = 0; j < scanLen; j++)
                    {
                        double p = j - halfLen + 0.5;
                        SamplePixel(src, scanCx + p * sdx, scanCy + p * sdy, out float val);
                        profile[j] = val;
                    }

                    // 找边缘：根据 EdgeSelector 选第一个/最后一个符合方向且超阈值的边
                    float[] grad = new float[scanLen];
                    int? selectedIdx = null;
                    float selectedGrad = 0;
                    for (int j = 1; j < scanLen - 1; j++)
                    {
                        grad[j] = profile[j + 1] - profile[j - 1];
                        bool dirMatch = direction == 2 ||
                                        direction == 0 && grad[j] > 0 ||
                                        direction == 1 && grad[j] < 0;
                        if (dirMatch && Math.Abs(grad[j]) >= threshold)
                        {
                            if (edgeSelector == 0)
                            { selectedIdx = j; selectedGrad = grad[j]; break; }
                            else
                            { selectedIdx = j; selectedGrad = grad[j]; }
                        }
                    }

                    if (selectedIdx.HasValue && Math.Abs(selectedGrad) / 2.55f >= minScore)
                    {
                        float e0 = grad[Math.Max(selectedIdx.Value - 1, 0)];
                        float e2 = grad[Math.Min(selectedIdx.Value + 1, scanLen - 1)];
                        float subPixelOffset = 0;
                        float denom = e0 - 2 * selectedGrad + e2;
                        if (Math.Abs(denom) > 1e-6f)
                            subPixelOffset = Math.Clamp(0.5f * (e0 - e2) / denom, -1.0f, 1.0f);

                        float edgePos = Math.Clamp(selectedIdx.Value + subPixelOffset, 0.5f, scanLen - 1.5f);
                        float score = Math.Abs(selectedGrad) / 2.55f;
                        if (score >= minScore)
                        {
                            double offset = edgePos - halfLen + 0.5;
                            edgePoints.Add(new Point2d(scanCx + offset * sdx, scanCy + offset * sdy));
                        }
                    }
                }
                allEdgePoints.Add(edgePoints);
            }

            // 画扫描线 — 写入 DrawCommands（不管拟合是否成功都要画）
            foreach (var (sx0, sy0, sx1, sy1, dirX, dirY) in allCaliperLines)
            {
                func.DrawCommands.Add(DrawCommand.Line(sx0, sy0, sx1, sy1, System.Windows.Media.Color.FromArgb(128, 0, 255, 255)));
                double adx = sx1 - sx0, ady = sy1 - sy0;
                double alen = Math.Sqrt(adx * adx + ady * ady);
                if (alen > 3)
                {
                    double du = dirX, dv = dirY;
                    double hs = Math.Min(alen * 0.3, 8);
                    func.DrawCommands.Add(DrawCommand.Line(sx1, sy1, sx1 - hs * (du * 0.7 - dv * 0.7), sy1 - hs * (dv * 0.7 + du * 0.7), System.Windows.Media.Color.FromArgb(128, 0, 255, 255)));
                    func.DrawCommands.Add(DrawCommand.Line(sx1, sy1, sx1 - hs * (du * 0.7 + dv * 0.7), sy1 - hs * (dv * 0.7 - du * 0.7), System.Windows.Media.Color.FromArgb(128, 0, 255, 255)));
                }
            }

            foreach (var epList in allEdgePoints)
                foreach (var pt in epList)
                    func.DrawCommands.Add(DrawCommand.Circle(pt.X, pt.Y, 3, System.Windows.Media.Color.FromArgb(128, 0, 255, 0)));

            _ = func.RenderDrawAsync();
            // 拟合每条边的直线（RANSAC 剔除离群点，方向从内点计算）
            var lines = new Line2D[4];
            var colors = new[] { new Scalar(0, 0, 255), new Scalar(0, 255, 0), new Scalar(255, 0, 0), new Scalar(0, 255, 255) };
            for (int ei = 0; ei < 4; ei++)
            {
                if (allEdgePoints[ei].Count < 2)
                    throw new Exception($"边{ei} 仅找到 {allEdgePoints[ei].Count} 个边缘点！");
                var pts = allEdgePoints[ei];

                // RANSAC 找内点
                var inls = RansacInliers(pts, inlierTh);

                // Cv2.FitLine 求精确方向
                var fp = inls.Select(p => new Point2f((float)p.X, (float)p.Y)).ToArray();
                var fl = Cv2.FitLine(fp, DistanceTypes.L2, 0, 0.01, 0.01);
                double ux = fl.Vx, uy = fl.Vy;
                lines[ei] = new Line2D(fl.X1, fl.Y1, fl.X1 + fl.Vx * 100, fl.Y1 + fl.Vy * 100);

                // 存首尾两个内点用于求交点
                lines[ei] = new Line2D(inls[0].X, inls[0].Y, inls[^1].X, inls[^1].Y);

                // 画拟合线（沿边方向延伸到整条边长度）
                double mcx = inls.Average(p => p.X), mcy = inls.Average(p => p.Y);
                double vx = inls[^1].X - inls[0].X, vy = inls[^1].Y - inls[0].Y;
                double vlen = Math.Sqrt(vx * vx + vy * vy);
                if (vlen > 1)
                {
                    double dx = vx / vlen, dy = vy / vlen;
                    double p0 = ((inls[0].X - mcx) * dx + (inls[0].Y - mcy) * dy);
                    double pN = ((inls[^1].X - mcx) * dx + (inls[^1].Y - mcy) * dy);
                    double ext = 50;
                    var edgeColors = new[] { System.Windows.Media.Color.FromArgb(128, 0, 0, 255), System.Windows.Media.Color.FromArgb(128, 0, 255, 0), System.Windows.Media.Color.FromArgb(128, 255, 0, 0), System.Windows.Media.Color.FromArgb(128, 0, 255, 255) };
                    func.DrawCommands.Add(DrawCommand.Line(mcx + (p0 - ext) * dx, mcy + (p0 - ext) * dy,
                                                            mcx + (pN + ext) * dx, mcy + (pN + ext) * dy, edgeColors[ei], 2));
                }
            }

            // 求交点（CSDN 公式：Line2D 的两点 (Vx,Vy) 和 (X1,Y1)）
            var corners = new Point2d[4];
            for (int i = 0; i < 4; i++)
            {
                var l1 = lines[i]; var l2 = lines[(i + 1) % 4];
                double k1 = (l1.Y1 - l1.Vy) / (l1.X1 - l1.Vx);
                double k2 = (l2.Y1 - l2.Vy) / (l2.X1 - l2.Vx);
                double ix = (k1 * l1.Vx - l1.Vy - k2 * l2.Vx + l2.Vy) / (k1 - k2);
                double iy = (k1 * k2 * (l1.Vx - l2.Vx) + k1 * l2.Vy - k2 * l1.Vy) / (k1 - k2);
                corners[i] = new Point2d(ix, iy);
            }

            // 画 4 个角点 — 写入 DrawCommands
            for (int i = 0; i < 4; i++)
                func.DrawCommands.Add(DrawCommand.FilledCircle(corners[i].X, corners[i].Y, 6, System.Windows.Media.Color.FromArgb(128, 255, 255, 0)));

            // 从 4 个角点直接算矩形参数
            double fitCx = (corners[0].X + corners[1].X + corners[2].X + corners[3].X) / 4;
            double fitCy = (corners[0].Y + corners[1].Y + corners[2].Y + corners[3].Y) / 4;
            double fitW = (Hypot(corners[0], corners[1]) + Hypot(corners[2], corners[3])) / 2;
            double fitH = (Hypot(corners[1], corners[2]) + Hypot(corners[3], corners[0])) / 2;
            double fitAngle = Math.Atan2(corners[1].Y - corners[0].Y, corners[1].X - corners[0].X) * 180 / Math.PI;

            // 内点统计（仅显示，不阻断）
            int inliers = allEdgePoints.Sum(ep => ep.Count(p => PointToRectDist(p, fitCx, fitCy, fitW, fitH, fitAngle * Math.PI / 180) <= inlierTh));

            // 画拟合旋转矩形 — 写入 DrawCommands
            var rr = new RotatedRect(new Point2f((float)fitCx, (float)fitCy), new Size2f((float)fitW, (float)fitH), (float)fitAngle);
            var rrPts = rr.Points();
            for (int i = 0; i < 4; i++)
                func.DrawCommands.Add(DrawCommand.Line(rrPts[i].X, rrPts[i].Y, rrPts[(i+1)%4].X, rrPts[(i+1)%4].Y, System.Windows.Media.Color.FromArgb(128, 255, 0, 255), 3));
            func.DrawCommands.Add(DrawCommand.FilledCircle(fitCx, fitCy, 4, System.Windows.Media.Color.FromArgb(128, 255, 0, 255)));
            // 保存结果
            func.Params.ResultDoubles["InlierCount"] = inliers;

            // 模板模式：保存当前找到的位置为模板
            if (item.StringParams.TryGetValue("ComputeTemplate", out var ct) && ct == "1")
            {
                item.DoubleParams["TemplateRectCX"] = Math.Round(fitCx, 3);
                item.DoubleParams["TemplateRectCY"] = Math.Round(fitCy, 3);
                item.DoubleParams["TemplateRectAngle"] = Math.Round(fitAngle, 3);
                item.Flow.Done = true;
                return true;
            }

            // 有模板时计算偏移
            double offsetX = 0, offsetY = 0, offsetAngle = 0;
            if (item.DoubleParams.TryGetValue("TemplateRectCX", out double tcx) &&
                item.DoubleParams.TryGetValue("TemplateRectCY", out double tcy) &&
                item.DoubleParams.TryGetValue("TemplateRectAngle", out double ta))
            {
                offsetX = Math.Round(fitCx - tcx, 3);
                offsetY = Math.Round(fitCy - tcy, 3);
                offsetAngle = Math.Round(fitAngle - ta, 3);
                func.Params.ResultDoubles["RectOffsetX"] = offsetX;
                func.Params.ResultDoubles["RectOffsetY"] = offsetY;
                func.Params.ResultDoubles["RectOffsetAngle"] = offsetAngle;
            }

            // 写入局部变量
            var variable = func.Params.Variables.FirstOrDefault(v => v.Name == resultVar);
            if (variable == null)
            {
                variable = new LocalVariableItem(resultVar, "Rect", new Rect(new Pos(), 0, 0));
                System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(variable));
            }
            variable.RawValue = new Rect(
                new Pos(Math.Round(fitCx, 3), Math.Round(fitCy, 3), 0, Math.Round(fitAngle, 3)),
                Math.Round(fitW, 3), Math.Round(fitH, 3));

            // 写入偏移变量
            if (item.DoubleParams.TryGetValue("TemplateRectCX", out _))
            {
                var offsetVarName = item.StringParams.TryGetValue("RectOffsetVar", out var ov) && !string.IsNullOrEmpty(ov) ? ov : "找矩形_Offset";
                var offsetVar = func.Params.Variables.FirstOrDefault(v => v.Name == offsetVarName);
                if (offsetVar == null) { offsetVar = new LocalVariableItem(offsetVarName, "Pos", new Pos()); System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(offsetVar)); }
                offsetVar.RawValue = new Pos(offsetX, offsetY, 0, offsetAngle);
            }

            item.Flow.Done = true;
            _ = func.RenderDrawAsync();
            return true;
        }

        private static void SamplePixel(Mat src, double x, double y, out float val)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            int x1 = x0 + 1, y1 = y0 + 1;
            double fx = x - x0, fy = y - y0;
            x0 = Math.Clamp(x0, 0, src.Width - 1); x1 = Math.Clamp(x1, 0, src.Width - 1);
            y0 = Math.Clamp(y0, 0, src.Height - 1); y1 = Math.Clamp(y1, 0, src.Height - 1);
            double v00 = src.At<byte>(y0, x0), v10 = src.At<byte>(y0, x1);
            double v01 = src.At<byte>(y1, x0), v11 = src.At<byte>(y1, x1);
            val = (float)((1 - fy) * ((1 - fx) * v00 + fx * v10) + fy * ((1 - fx) * v01 + fx * v11));
        }

        private static double Hypot(Point2d a, Point2d b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double PointToRectDist(Point2d p, double cx, double cy, double w, double h, double rad)
        {
            double ca = Math.Cos(rad), sa = Math.Sin(rad);
            double dx = p.X - cx, dy = p.Y - cy;
            double rx = dx * ca + dy * sa, ry = -dx * sa + dy * ca;
            double hw = w / 2, hh = h / 2;
            double ox = Math.Max(Math.Abs(rx) - hw, 0), oy = Math.Max(Math.Abs(ry) - hh, 0);
            if (ox == 0 && oy == 0) return -Math.Min(hw - Math.Abs(rx), hh - Math.Abs(ry));
            return Math.Sqrt(ox * ox + oy * oy);
        }

        private static List<Point2d> RansacInliers(List<Point2d> points, double threshold)
        {
            int n = points.Count;
            if (n <= 2) return points;

            var rng = new Random(42);
            int bestCnt = 0;
            double bestVx = 0, bestVy = 0, bestX = 0, bestY = 0;

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
                {
                    double dx = points[i].X - points[i1].X, dy = points[i].Y - points[i1].Y;
                    if (Math.Abs(dx * vy - dy * vx) <= threshold) cnt++;
                }
                if (cnt > bestCnt) { bestCnt = cnt; bestVx = vx; bestVy = vy; bestX = points[i1].X; bestY = points[i1].Y; }
            }

            // 返回内点（沿线方向排序）
            var result = points.Where(p =>
                Math.Abs((p.X - bestX) * bestVy - (p.Y - bestY) * bestVx) <= threshold).ToList();
            result.Sort((a, b) => (a.X * bestVx + a.Y * bestVy).CompareTo(b.X * bestVx + b.Y * bestVy));
            return result;
        }
    }
}
