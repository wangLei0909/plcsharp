using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    /// <summary>
    /// 卡尺找圆Handler
    /// </summary>
    public class CaliperFindCircleHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.卡尺找圆;

        /// <summary>
        /// 执行
        /// </summary>
        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src ?? throw new Exception("请先获取图片！");

            // 读取圆形 ROI 参数
            if (!item.DoubleParams.TryGetValue("CircleCenterX", out double cx) ||
                !item.DoubleParams.TryGetValue("CircleCenterY", out double cy) ||
                !item.DoubleParams.TryGetValue("CircleRadius", out double radius))
                throw new Exception("圆形ROI未配置，请先画圆！");

            // 读取卡尺参数
            var threshold = item.IntParams.TryGetValue("Threshold", out int t) ? Math.Max(t, 1) : 30;
            var numCalipers = item.IntParams.TryGetValue("NumCalipers", out int nc) ? Math.Max(nc, 4) : 50;
            var caliperLength = item.IntParams.TryGetValue("CaliperLength", out int cl) ? Math.Max(cl, 3) : 100;
            var direction = item.IntParams.TryGetValue("Direction", out int dir) ? dir : 0;
            var caliperDirection = item.IntParams.TryGetValue("CaliperDirection", out int cd) ? cd : 0;
            var edgeSelector = item.IntParams.TryGetValue("EdgeSelector", out int es) ? Math.Clamp(es, 0, 1) : 0;
            var minScore = item.IntParams.TryGetValue("MinScore", out int ms) ? Math.Clamp(ms, 0, 100) : 10;
            var minInliers = item.IntParams.TryGetValue("MinInliers", out int mi) ? Math.Max(mi, 3) : 10;

            var inlierTh = item.DoubleParams.TryGetValue("RansacTh", out double ith) ? Math.Max(ith, 0.1) : 5;
            var resultVar = item.StringParams.TryGetValue("CircleResultVar", out var rv) && !string.IsNullOrEmpty(rv)
                ? rv : "卡尺找圆_Circle";

            if (src.Channels() == 3)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            double halfLen = caliperLength / 2.0;
            int scanLen = caliperLength;

            var edgePoints = new List<Point2d>();
            var edgeScores = new List<float>();
            // 可视化 — 写入 DrawCommands

            var cyan = System.Windows.Media.Color.FromArgb(128, 0, 255, 255);
            var lime = System.Windows.Media.Color.FromArgb(128, 0, 255, 0);
            var magenta = System.Windows.Media.Color.FromArgb(128, 255, 0, 255);
            // 沿圆周均布卡尺，径向扫描
            for (int k = 0; k < numCalipers; k++)
            {
                double angle = 2.0 * Math.PI * k / numCalipers;
                double cosA = Math.Cos(angle), sinA = Math.Sin(angle);

                // 卡尺中心在圆周上
                double px = cx + radius * cosA;
                double py = cy + radius * sinA;

                // 径向方向（从圆心指向外）
                double rdx = cosA, rdy = sinA;

                float[] profile = new float[scanLen];
                for (int j = 0; j < scanLen; j++)
                {
                    // 两种方向都沿圆周对称扫描，向内向外各一半
                    // 从内向外：j=0 在最内侧，j=scanLen-1 在最外侧
                    // 从外向内：j=0 在最外侧，j=scanLen-1 在最内侧
                    double pos = caliperDirection == 1
                        ? halfLen - 0.5 - j
                        : j - halfLen + 0.5;
                    double spx = px + pos * rdx;
                    double spy = py + pos * rdy;

                    int x0 = (int)Math.Floor(spx), y0 = (int)Math.Floor(spy);
                    int x1 = x0 + 1, y1 = y0 + 1;
                    double fx = spx - x0, fy = spy - y0;

                    x0 = Math.Clamp(x0, 0, src.Width - 1);
                    x1 = Math.Clamp(x1, 0, src.Width - 1);
                    y0 = Math.Clamp(y0, 0, src.Height - 1);
                    y1 = Math.Clamp(y1, 0, src.Height - 1);

                    double v00 = src.At<byte>(y0, x0);
                    double v10 = src.At<byte>(y0, x1);
                    double v01 = src.At<byte>(y1, x0);
                    double v11 = src.At<byte>(y1, x1);

                    profile[j] = (float)((1 - fy) * ((1 - fx) * v00 + fx * v10)
                                       + fy * ((1 - fx) * v01 + fx * v11));
                }

                // 梯度检测：根据 EdgeSelector 选择第一个或最后一个符合条件的边缘
                int? selectedIdx = null;
                float selectedGrad = 0;
                float[] grad = new float[scanLen];
                for (int j = 1; j < scanLen - 1; j++)
                {
                    grad[j] = profile[j + 1] - profile[j - 1];
                    bool dirMatch = direction == 2 ||
                                    direction == 0 && grad[j] > 0 ||
                                    direction == 1 && grad[j] < 0;
                    float absGrad = Math.Abs(grad[j]);
                    if (dirMatch && absGrad >= threshold)
                    {
                        if (edgeSelector == 0)
                        {
                            // 找第一个
                            selectedIdx = j; selectedGrad = grad[j];
                            break;
                        }
                        else
                        {
                            // 找最后一个
                            selectedIdx = j; selectedGrad = grad[j];
                        }
                    }
                }

                if (selectedIdx.HasValue)
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
                        // 边缘物理位置：与 profile 采样使用一致的 pos 公式
                        double edgeOffset = caliperDirection == 1
                            ? halfLen - 0.5 - edgePos
                            : edgePos - halfLen + 0.5;
                        edgePoints.Add(new Point2d(px + edgeOffset * rdx, py + edgeOffset * rdy));
                        edgeScores.Add(score);
                    }
                }
            }
            for (int k = 0; k < numCalipers; k++)
            {
                double angle = 2.0 * Math.PI * k / numCalipers;
                double cosA = Math.Cos(angle), sinA = Math.Sin(angle);
                double px = cx + radius * cosA;
                double py = cy + radius * sinA;
                double rdx = cosA, rdy = sinA;
                double sx0 = px - halfLen * rdx, sy0 = py - halfLen * rdy;
                double sx1 = px + halfLen * rdx, sy1 = py + halfLen * rdy;
                func.DrawCommands.Add(DrawCommand.Line(sx0, sy0, sx1, sy1, cyan));
                double adx = sx1 - sx0, ady = sy1 - sy0;
                double alen = Math.Sqrt(adx * adx + ady * ady);
                if (alen > 3)
                {
                    double du, dv;
                    double arrowX, arrowY;
                    if (caliperDirection == 1)
                    {
                        du = (sx0 - sx1) / alen; dv = (sy0 - sy1) / alen;
                        arrowX = sx0; arrowY = sy0;
                    }
                    else
                    {
                        du = adx / alen; dv = ady / alen;
                        arrowX = sx1; arrowY = sy1;
                    }
                    double hs = Math.Min(alen * 0.3, 8);
                    func.DrawCommands.Add(DrawCommand.Line(arrowX, arrowY, arrowX - hs * (du * 0.7 - dv * 0.7), arrowY - hs * (dv * 0.7 + du * 0.7), cyan));
                    func.DrawCommands.Add(DrawCommand.Line(arrowX, arrowY, arrowX - hs * (du * 0.7 + dv * 0.7), arrowY - hs * (dv * 0.7 - du * 0.7), cyan));
                }
            }
            foreach (var pt in edgePoints)
                func.DrawCommands.Add(DrawCommand.Circle(pt.X, pt.Y, 5, lime));

            _ = func.RenderDrawAsync();
            if (edgePoints.Count < 3)
                throw new Exception($"仅找到 {edgePoints.Count} 个边缘点，至少需要3个！");

            // RANSAC 拟合圆
            var circle = FitCircleRansac(edgePoints, maxIterations: 200, inlierThreshold: inlierTh);

            // 统计内点数
            int ransacInliers = edgePoints.Count(p =>
            {
                double dx = p.X - circle.CenterX, dy = p.Y - circle.CenterY;
                return Math.Abs(Math.Sqrt(dx * dx + dy * dy) - circle.Radius) <= inlierTh;
            });

            if (ransacInliers < minInliers)
                throw new Exception($"内点数不足 ({ransacInliers} < {minInliers})，请调整 RANSAC 阈值或检查边缘点！");


            func.DrawCommands.Add(DrawCommand.Circle(circle.CenterX, circle.CenterY, circle.Radius, magenta, 2));
            func.DrawCommands.Add(DrawCommand.FilledCircle(circle.CenterX, circle.CenterY, 3, magenta));
            // 模板模式：保存当前圆心半径
            if (item.StringParams.TryGetValue("ComputeTemplate", out var ct) && ct == "1")
            {
                item.DoubleParams["TemplateCircleCX"] = Math.Round(circle.CenterX, 3);
                item.DoubleParams["TemplateCircleCY"] = Math.Round(circle.CenterY, 3);
                item.DoubleParams["TemplateCircleR"] = Math.Round(circle.Radius, 3);
                item.Flow.Done = true;
                return true;
            }

            // 有模板时计算偏移
            if (item.DoubleParams.TryGetValue("TemplateCircleCX", out double tcx) &&
                item.DoubleParams.TryGetValue("TemplateCircleCY", out double tcy) &&
                item.DoubleParams.TryGetValue("TemplateCircleR", out double tr))
            {
                double offX = Math.Round(circle.CenterX - tcx, 3);
                double offY = Math.Round(circle.CenterY - tcy, 3);
                double offR = Math.Round(circle.Radius - tr, 3);
                func.Params.ResultDoubles["CircleOffsetX"] = offX;
                func.Params.ResultDoubles["CircleOffsetY"] = offY;
                func.Params.ResultDoubles["CircleOffsetR"] = offR;

                var offVarName = item.StringParams.TryGetValue("CircleOffsetVar", out var ov) && !string.IsNullOrEmpty(ov) ? ov : "找圆_Offset";
                var offVar = func.Params.Variables.FirstOrDefault(v => v.Name == offVarName);
                if (offVar == null) { offVar = new LocalVariableItem(offVarName, "Pos", new Pos()); System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(offVar)); }
                offVar.RawValue = new Pos(offX, offY, offR, 0);
            }

            // 保存结果到变量
            var variable = func.Params.Variables.FirstOrDefault(v => v.Name == resultVar);
            if (variable == null)
            {
                variable = new LocalVariableItem(resultVar, "Circle", new Circle(new Pos(), 0));
                System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(variable));
            }
            variable.RawValue = new Circle(
                new Pos(Math.Round(circle.CenterX, 3), Math.Round(circle.CenterY, 3), 0, 0),
                Math.Round(circle.Radius, 3));

            item.Flow.Done = true;
            _ = func.RenderDrawAsync();
            return true;
        }

        /// <summary>
        /// RANSAC 拟合圆。从点集中随机取3点计算圆，取内点最多的结果。
        /// </summary>
        private static CircleResult FitCircleRansac(List<Point2d> points, int maxIterations, double inlierThreshold)
        {
            int n = points.Count;
            if (n < 3) throw new Exception("至少需要 3 个点");

            var rng = new Random(42);
            int bestInlierCount = 0;
            double bestCx = 0, bestCy = 0, bestR = 0;

            int iterations = Math.Min(maxIterations, n * (n - 1) * (n - 2) / 6);
            for (int iter = 0; iter < iterations; iter++)
            {
                int i1 = rng.Next(n), i2 = rng.Next(n), i3 = rng.Next(n);
                while (i2 == i1) i2 = rng.Next(n);
                while (i3 == i1 || i3 == i2) i3 = rng.Next(n);

                var p1 = points[i1]; var p2 = points[i2]; var p3 = points[i3];
                if (!TryFitCircle(p1, p2, p3, out double ccx, out double ccy, out double cr))
                    continue;

                int inlierCount = 0;
                for (int i = 0; i < n; i++)
                {
                    double dx = points[i].X - ccx, dy = points[i].Y - ccy;
                    double dist = Math.Abs(Math.Sqrt(dx * dx + dy * dy) - cr);
                    if (dist <= inlierThreshold) inlierCount++;
                }

                if (inlierCount > bestInlierCount)
                {
                    bestInlierCount = inlierCount;
                    bestCx = ccx; bestCy = ccy; bestR = cr;
                }
            }

            if (bestInlierCount < 3)
                throw new Exception("RANSAC 拟合圆失败：无法找到足够的内点！");

            // 用所有内点重新拟合（最小二乘）
            var inliers = new List<Point2d>();
            for (int i = 0; i < n; i++)
            {
                double dx = points[i].X - bestCx, dy = points[i].Y - bestCy;
                if (Math.Abs(Math.Sqrt(dx * dx + dy * dy) - bestR) <= inlierThreshold)
                    inliers.Add(points[i]);
            }

            if (inliers.Count >= 3)
            {
                var refined = FitCircleLeastSquares(inliers);
                return refined;
            }

            return new CircleResult(bestCx, bestCy, bestR);
        }

        /// <summary>
        /// 从 3 个点计算圆（圆心和半径）
        /// </summary>
        private static bool TryFitCircle(Point2d p1, Point2d p2, Point2d p3,
            out double cx, out double cy, out double r)
        {
            cx = cy = r = 0;
            double a = p2.X - p1.X, b = p2.Y - p1.Y;
            double c = p3.X - p1.X, d = p3.Y - p1.Y;
            double e = a * (p1.X + p2.X) + b * (p1.Y + p2.Y);
            double f = c * (p1.X + p3.X) + d * (p1.Y + p3.Y);
            double g = 2.0 * (a * (p3.Y - p2.Y) - b * (p3.X - p2.X));

            if (Math.Abs(g) < 1e-10) return false;

            cx = (d * e - b * f) / g;
            cy = (a * f - c * e) / g;
            r = Math.Sqrt((p1.X - cx) * (p1.X - cx) + (p1.Y - cy) * (p1.Y - cy));
            return r > 0.1 && !double.IsNaN(r) && !double.IsInfinity(r);
        }

        /// <summary>
        /// 最小二乘拟合圆（基于所有内点）
        /// </summary>
        private static CircleResult FitCircleLeastSquares(List<Point2d> points)
        {
            int n = points.Count;
            double sumX = 0, sumY = 0, sumX2 = 0, sumY2 = 0, sumX3 = 0, sumY3 = 0, sumXY = 0, sumX2Y = 0, sumXY2 = 0;

            for (int i = 0; i < n; i++)
            {
                double x = points[i].X, y = points[i].Y;
                double x2 = x * x, y2 = y * y;
                sumX += x; sumY += y;
                sumX2 += x2; sumY2 += y2;
                sumX3 += x2 * x; sumY3 += y2 * y;
                sumXY += x * y;
                sumX2Y += x2 * y; sumXY2 += x * y2;
            }

            double A = n * sumX2 - sumX * sumX;
            double B = n * sumXY - sumX * sumY;
            double C = n * sumY2 - sumY * sumY;
            double D = 0.5 * (n * sumX3 - sumX * sumX2 + n * sumXY2 - sumX * sumY2);
            double E = 0.5 * (n * sumY3 - sumY * sumY2 + n * sumX2Y - sumY * sumX2);

            double det = A * C - B * B;
            if (Math.Abs(det) < 1e-10)
            {
                // 退化情况，用平均值
                double avgCx = sumX / n, avgCy = sumY / n;
                double avgR = points.Average(p => Math.Sqrt((p.X - avgCx) * (p.X - avgCx) + (p.Y - avgCy) * (p.Y - avgCy)));
                return new CircleResult(avgCx, avgCy, avgR);
            }

            double fitCx = (D * C - B * E) / det;
            double fitCy = (A * E - B * D) / det;
            double fitR = points.Average(p => Math.Sqrt((p.X - fitCx) * (p.X - fitCx) + (p.Y - fitCy) * (p.Y - fitCy)));

            return new CircleResult(fitCx, fitCy, fitR);
        }

        private readonly record struct CircleResult(double CenterX, double CenterY, double Radius);
    }
}
