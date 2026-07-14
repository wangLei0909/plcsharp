using DryIoc.ImTools;

using OpenCvSharp;
using OpenCvSharp.Dnn;
using static OpenCvSharp.Cv2;
namespace PLCSharp.VVMs.Vision.VisionFlowHandler
{

    /// <summary>
    /// 矩阵Extension
    /// </summary>
    public static class MatExtension
    {        /// <summary>
             /// 随机抽取4个点计算单应性矩阵并重投影，比较坐标，记录正确点数量；多次重复，将正确点数量最多的当做正确匹配
             /// </summary>
             /// <param name="dMatches"></param>
             /// <param name="queryKeyPoints"></param>
             /// <param name="trainKeyPoint"></param>
             /// <returns></returns>
        public static (List<DMatch>, List<Point2d>, List<Point2d>) Ransac(DMatch[] dMatches, KeyPoint[] queryKeyPoints, KeyPoint[] trainKeyPoint)
        {
            List<DMatch> reList = [];
            List<Point2d> src1Pts = [];
            List<Point2d> dst1Pts = [];
            List<Point2d> srcPoints = [];
            List<Point2d> dstPoints = [];

            for (int i = 0; i < dMatches.Length; i++)
            {
                srcPoints.Add(new Point2d(queryKeyPoints[dMatches[i].QueryIdx].Pt.X, queryKeyPoints[dMatches[i].QueryIdx].Pt.Y));
                dstPoints.Add(new Point2d(trainKeyPoint[dMatches[i].TrainIdx].Pt.X, trainKeyPoint[dMatches[i].TrainIdx].Pt.Y));
            }
            Mat inliersMask = new();
            _ = Cv2.FindHomography(srcPoints, dstPoints, HomographyMethods.Ransac, 3, inliersMask);
            _ = inliersMask.GetArray(out byte[] inliersArray);
            for (int i = 0; i < inliersArray.Length; i++)
            {
                if (inliersArray[i] != 0)
                {
                    reList.Add(dMatches[i]);
                    src1Pts.Add(srcPoints[i]);
                    dst1Pts.Add(dstPoints[i]);
                }
            }
            return (reList, src1Pts, dst1Pts);
        }

        /// <summary>
        /// 获取直方图
        /// </summary>
        /// <param name="dif">dif</param>
        /// <param name="max">最大</param>
        /// <param name="maxindex">maxindex</param>
        /// <returns>返回结果</returns>
        public static Mat GetHist(this Mat dif, ref int max, ref int maxindex)
        {
            Mat reduceColumn = new();
            Cv2.Reduce(dif, reduceColumn, ReduceDimension.Column, ReduceTypes.Avg, -1);

            Mat hist = new(reduceColumn.Height, 512, MatType.CV_8UC1, Scalar.Black);
            for (int i = 0; i < reduceColumn.Height; i++)
            {
                int len = reduceColumn.Get<byte>(i, 0);
                if (len != 0)
                {
                    Cv2.Line(hist, new(512 - len, i), new(511, i), Scalar.White);
                }
                if (len > max)
                {
                    max = len;
                    maxindex = i;
                }
            }
            reduceColumn.Dispose();
            return hist;
        }
        //画旋转矩形
        /// <summary>
        /// 绘制Rotated矩形
        /// </summary>
        /// <param name="mat">图像矩阵</param>
        /// <param name="rr">rr</param>
        /// <param name="scalar">scalar</param>
        /// <param name="thickness">thickness</param>
        public static void DrawRotatedRect(this Mat mat, RotatedRect rr, Scalar scalar, int thickness = 2)
        {
            var P = rr.Points();
            for (int j = 0; j <= 3; j++)
            {
                Cv2.Line(mat, (Point)P[j], (Point)P[(j + 1) % 4], scalar, thickness);
            }
        }

        /// <summary>
        /// 填充多边形
        /// </summary>
        /// <param name="src">源图像</param>
        /// <param name="points">坐标点集合</param>
        public static void FillPolygon(this Mat src, List<Point> points)
        {
            List<List<Point>> polygons = new() { points };
            Cv2.FillPoly(src, polygons, Scalar.White);
        }
        /// <summary>
        /// 填充多边形
        /// </summary>
        /// <param name="src">源图像</param>
        /// <param name="rr">rr</param>
        public static void FillPolygon(this Mat src, RotatedRect rr)
        {
            var P = rr.Points().Select(p => new Point(p.X, p.Y)).ToArray();
            var pp = new Point[1][] { P };
            Cv2.FillPoly(src, pp, Scalar.White);
        }
        /// <summary>
        /// 绘制多边形
        /// </summary>
        /// <param name="mat">图像矩阵</param>
        /// <param name="points">坐标点集合</param>
        /// <param name="thickness">thickness</param>
        public static void DrawPolygon(this Mat mat, Point2f[] points, int thickness = 1)
        {
            if (points.Length < 2) return;

            for (int j = 0; j <= points.Length - 1; j++)
            {
                Cv2.Line(mat, (Point)points[j], (Point)points[(j + 1) % points.Length], Scalar.RandomColor(), thickness);
            }
        }
        /// <summary>
        /// 绘制多边形
        /// </summary>
        /// <param name="mat">图像矩阵</param>
        /// <param name="points">坐标点集合</param>
        /// <param name="thickness">thickness</param>
        public static void DrawPolygon(this Mat mat, List<Point> points, int thickness = 1)
        {
            if (points.Count < 2) return;

            for (int j = 0; j <= points.Count - 1; j++)
            {
                Cv2.Line(mat, points[j], points[(j + 1) % points.Count], Scalar.RandomColor(), thickness);
            }
        }
        /// <summary>
        /// 绘制角度
        /// </summary>
        /// <param name="img">img</param>
        /// <param name="p0">p0</param>
        /// <param name="p1">p1</param>
        /// <param name="p2">p2</param>
        /// <param name="radius">半径</param>
        public static void DrawAngle(this Mat img, Point2d p0, Point2d p1, Point2d p2, double radius)
        {
            // 计算直线的角度
            double angle1 = Math.Atan2(-(p1.Y - p0.Y), p1.X - p0.X) * 180 / Cv2.PI;
            double angle2 = Math.Atan2(-(p2.Y - p0.Y), p2.X - p0.X) * 180 / Cv2.PI;
            // 计算主轴的角度
            double angle = angle1 <= 0 ? -angle1 : 360 - angle1;
            // 计算圆弧的结束角度
            double end_angle = (angle2 < angle1) ? (angle1 - angle2) : (360 - (angle2 - angle1));
            // 画圆弧
            Cv2.Ellipse(img, (Point)p0, new Size(radius, radius), angle, 0, end_angle, Scalar.RandomColor(), 2);
            //string a = (angle-end_angle).ToString();
            string a = end_angle.ToString("F3");
            Cv2.PutText(img, a, (Point)p0, HersheyFonts.HersheyDuplex, 0.8d, Scalar.Red);
        }
        /// <summary>
        /// 获取变换点位
        /// </summary>
        /// <param name="rot">仿射变换矩阵</param>
        /// <param name="point">点位</param>
        /// <returns>返回结果</returns>
        public static Point2d GetTransPoint(Mat rot, Point2d point)
        {
            var x = point.X * rot.At<double>(0, 0) + point.Y * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var y = point.X * rot.At<double>(1, 0) + point.Y * rot.At<double>(1, 1) + rot.At<double>(1, 2);
            return new Point2d(x, y);
        }
        /// <summary>
        /// 仿射变换
        /// </summary>
        /// <param name="src">输入</param>
        /// <param name="center">中心</param>
        /// <param name="angle">角度</param>
        /// <returns> 返回仿射变换后的完整图形 </returns>
        public static Mat Rotate(this Mat src, float angle)
        {
            // angle 0-360
            if (angle == 0 || angle == 360) return src;
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            Mat dst = new();

            //变换矩阵
            //  cos (angle)  sin(angle)
            //  -sin(angle)  cos(angle)

            Mat rot = Cv2.GetRotationMatrix2D(new(0, 0), angle, 1);

            //X==0 Y==0
            var w1 = rot.At<double>(0, 2);
            var h1 = rot.At<double>(1, 2);

            //Y==0
            var w2 = src.Width * rot.At<double>(0, 0) + rot.At<double>(0, 2);
            var h2 = src.Width * rot.At<double>(1, 0) + rot.At<double>(1, 2);

            //x==0
            var w3 = src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h3 = src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            var w4 = src.Width * rot.At<double>(0, 0) + src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h4 = src.Width * rot.At<double>(1, 0) + src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            Point[] points = { new(w1, h1), new(w2, h2), new(w3, h3), new(w4, h4) };

            OpenCvSharp.Rect rRect = Cv2.BoundingRect(points);
            switch (angle)
            {
                case >= 0 and <= 90:
                    rot.Set<double>(0, 2, 0);
                    rot.Set<double>(1, 2, h1 - h2);
                    break;

                case > 90 and <= 180:
                    rot.Set<double>(0, 2, -w2 + w1);
                    rot.Set<double>(1, 2, rRect.Height);
                    break;

                case > 180 and <= 270:
                    rot.Set<double>(0, 2, rRect.Width);
                    rot.Set<double>(1, 2, h1 - h3);
                    break;

                case > 270:
                    rot.Set<double>(0, 2, w1 - w3);
                    rot.Set<double>(1, 2, 0);
                    break;
            }

            Cv2.WarpAffine(src, dst, rot, rRect.Size);
            rot.Dispose();
            return dst;
        }

        /// <summary>
        /// 旋转
        /// </summary>
        /// <param name="src">源图像</param>
        /// <param name="angle">角度</param>
        /// <param name="pointIn">点位In</param>
        /// <param name="pointOut">点位Out</param>
        /// <returns>返回结果</returns>
        public static Mat Rotate(this Mat src, float angle, Point pointIn, out Point pointOut)
        {
            pointOut = pointIn;
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            Mat dst = new();

            //变换矩阵
            //  cos (angle)  sin(angle)
            //  -sin(angle)  cos(angle)

            Mat rot = Cv2.GetRotationMatrix2D(new(0, 0), angle, 1);

            //X==0 Y==0
            var w1 = rot.At<double>(0, 2);
            var h1 = rot.At<double>(1, 2);

            //Y==0
            var w2 = src.Width * rot.At<double>(0, 0) + rot.At<double>(0, 2);
            var h2 = src.Width * rot.At<double>(1, 0) + rot.At<double>(1, 2);

            //x==0
            var w3 = src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h3 = src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            var w4 = src.Width * rot.At<double>(0, 0) + src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h4 = src.Width * rot.At<double>(1, 0) + src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            Point[] points = { new(w1, h1), new(w2, h2), new(w3, h3), new(w4, h4) };

            OpenCvSharp.Rect rRect = Cv2.BoundingRect(points);
            switch (angle)
            {
                case >= 0 and <= 90:
                    rot.Set<double>(0, 2, 0);
                    rot.Set<double>(1, 2, h1 - h2);
                    break;

                case > 90 and <= 180:
                    rot.Set<double>(0, 2, -w2 + w1);
                    rot.Set<double>(1, 2, rRect.Height);
                    break;

                case > 180 and <= 270:
                    rot.Set<double>(0, 2, rRect.Width);
                    rot.Set<double>(1, 2, h1 - h3);
                    break;

                case > 270:
                    rot.Set<double>(0, 2, w1 - w3);
                    rot.Set<double>(1, 2, 0);
                    break;
            }
            var x = pointIn.X * rot.At<double>(0, 0) + pointIn.Y * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var y = pointIn.X * rot.At<double>(1, 0) + pointIn.Y * rot.At<double>(1, 1) + rot.At<double>(1, 2);
            pointOut.X = (int)Math.Round(x, 0);
            pointOut.Y = (int)Math.Round(y, 0);
            Cv2.WarpAffine(src, dst, rot, rRect.Size);
            return dst;
        }

        /// <summary>
        /// 两线交点
        /// </summary>
        /// <param name="Line1"></param>
        /// <param name="Line2"></param>
        /// <param name="crossPoint"></param>
        public static void IntersectionPoint(Line2D Line1, Line2D Line2, out Point2d crossPoint)
        {
            // Vx Vy 与直线共线的归一化向量的XY分量,可以理解为线段的一个端点
            // X1 Y1 直线上某点的坐标 可以理解为线段的另一个端点

            //  如果是一条垂直线，计算斜率会发生除0错误，所以对线稍加修改，对结果影响不大
            if (Line1.X1 - Line1.Vx == 0)
            {
                Line1 = new Line2D(Line1.Vx, Line1.Vy, Line1.X1 + 0.1, Line1.Y1);
            }

            if (Line2.X1 - Line2.Vx == 0)
            {
                Line2 = new Line2D(Line2.Vx, Line2.Vy, Line2.X1 + 0.1, Line2.Y1);
            }
            //对于过两个点(Vx，Vy) 和 (X1，Y1)的直线，斜率为k=(Y1-Vy)/(X1-Vx)。
            double k1 = (Line1.Y1 - Line1.Vy) / (Line1.X1 - Line1.Vx);
            double k2 = (Line2.Y1 - Line2.Vy) / (Line2.X1 - Line2.Vx);

            //交点
            crossPoint.X = (k1 * Line1.Vx - Line1.Vy - k2 * Line2.Vx + Line2.Vy) / (k1 - k2);
            crossPoint.Y = (k1 * k2 * (Line1.Vx - Line2.Vx) + k1 * Line2.Vy - k2 * Line1.Vy) / (k1 - k2);
        }

        /// <summary>
        /// 直线SegmentPoint2Line2D
        /// </summary>
        /// <param name="line">直线</param>
        /// <returns>返回结果</returns>
        public static Line2D LineSegmentPoint2Line2D(LineSegmentPoint line)
        {
            return new Line2D(line.P1.X, line.P1.Y, line.P2.X, line.P2.Y);
        }

        /// <summary>
        /// 彩色转灰度
        /// </summary>
        /// <param name="src">源图像</param>
        /// <param name="gray">灰度</param>
        /// <returns>返回布尔值</returns>
        public static bool BGR2GRAY(this Mat src, out Mat gray)
        {
            gray = new(0, 0, MatType.CV_8UC1, Scalar.Black);
            if (src.Empty()) return false;
            if (src.Type() != MatType.CV_8UC3 && src.Type() != MatType.CV_8UC1) return false;

            if (src.Type() == MatType.CV_8UC3)
            {
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                gray = src.Clone();
            }
            return true;
        }


        /// <summary>
        /// 灰度转彩色
        /// </summary>
        /// <param name="src">源图像</param>
        /// <param name="bgr">BGR</param>
        /// <returns>返回布尔值</returns>
        public static bool GRAY2BGR(this Mat src, out Mat bgr)
        {
            bgr = new(0, 0, MatType.CV_8UC3, Scalar.Black);

            if (src.Empty()) return false;
            if (src.Type() != MatType.CV_8UC3 && src.Type() != MatType.CV_8UC1) return false;

            if (src.Type() == MatType.CV_8UC3)
            {
                bgr = src.Clone();
            }
            else
            {
                Cv2.CvtColor(src, bgr, ColorConversionCodes.GRAY2BGR);
            }
            return true;
        }

        // Checks if a matrix is a valid rotation matrix.
        // 检查一个矩阵是否是一个有效的旋转矩阵。
        private static bool IsRotationMatrix(Mat R)
        {
            Mat Rt = new();
            Cv2.Transpose(R, Rt);
            Mat shouldBeIdentity = Rt * R;
            Mat I = Mat.Eye(3, 3, shouldBeIdentity.Type());
            return Cv2.Norm(I, shouldBeIdentity) < 1e-6;
        }

        // https://blog.csdn.net/xiangxianghehe/article/details/102481769
        // Calculates rotation matrix to euler angles  计算旋转矩阵到欧拉角
        /// <summary>
        /// RotationMatrixTo欧拉Angles
        /// </summary>
        /// <param name="R">R</param>
        /// <param name="euler">欧拉</param>
        /// <returns>返回布尔值</returns>
        public static bool RotationMatrixToEulerAngles(Mat R, out Vec3d euler)
        {
            euler = new();

            if (!IsRotationMatrix(R)) return false;

            var sy = Math.Sqrt(R.At<double>(0, 0) * R.At<double>(0, 0) + R.At<double>(1, 0) * R.At<double>(1, 0));

            bool singular = sy < 1e-6;

            if (!singular)
            {
                euler.Item0 = Math.Atan2(R.At<double>(2, 1), R.At<double>(2, 2));
                euler.Item1 = Math.Atan2(-R.At<double>(2, 0), sy);
                euler.Item2 = Math.Atan2(R.At<double>(1, 0), R.At<double>(0, 0));
            }
            else
            {
                euler.Item0 = Math.Atan2(-R.At<double>(1, 2), R.At<double>(1, 1));
                euler.Item1 = Math.Atan2(-R.At<double>(2, 0), sy);
                euler.Item2 = 0;
            }

            return true;
        }

        //基于傅里叶变换的角度检测 https://blog.csdn.net/CSDN131137/article/details/103008744
        /// <summary>
        /// 获取DFT(傅里叶变换)角度
        /// </summary>
        /// <param name="src">源图像</param>
        /// <param name="DFT">DFT(傅里叶变换)</param>
        /// <returns>返回双精度值</returns>
        public static double GetDFTAngle(this Mat src, out Mat DFT)
        {
            //OpenCV中的DFT对图像尺寸有一定要求，
            //需要用GetOptimalDFTSize方法来找到合适的大小，
            //根据这个大小建立新的图像，把原图像拷贝过去，多出来的部分直接填充0。
            DFT = new Mat();
            var result = src.BGR2GRAY(out Mat gray);
            if (!result) return 0;

            int width = Cv2.GetOptimalDFTSize(src.Width);
            int height = Cv2.GetOptimalDFTSize(src.Height);
            var padded = new Mat(height, width, MatType.CV_8UC1, Scalar.Black);//扩展后的图像，单通道

            padded[0, src.Height, 0, src.Width] = gray.Clone();

            padded.ConvertTo(padded, MatType.CV_32FC1);

            Mat zeros = new Mat(padded.Size(), MatType.CV_32FC1) * 0f;
            Mat comImg = new();

            Mat[] paddeds = new[] { padded, zeros };
            //Merge into a double-channel image 合并成一个双通道图像
            Cv2.Merge(paddeds, comImg);

            Cv2.Dft(comImg, comImg);

            Cv2.Split(comImg, out Mat[] planes);
            Cv2.Magnitude(planes[0], planes[1], planes[0]);

            //计算幅值，转换到对数尺度(logarithmic scale)
            //Switch to logarithmic scale, for better visual results
            //M2=log(1+M1)
            Mat magMat = planes[0];
            magMat += Scalar.All(1);
            Cv2.Log(magMat, magMat);

            //Crop the spectrum
            //Width and height of magMat should be even, so that they can be divided by 2
            //-2 is 11111110 in binary system, operator & make sure width and height are always even
            magMat = magMat[new OpenCvSharp.Rect(0, 0, magMat.Cols & -2, magMat.Rows & -2)];

            //Rearrange the quadrants of Fourier image,
            //so that the origin is at the center of image,
            //and move the high frequency to the corners
            int cx = magMat.Cols / 2;
            int cy = magMat.Rows / 2;

            Mat q0 = new(magMat, new OpenCvSharp.Rect(0, 0, cx, cy));
            Mat q1 = new(magMat, new OpenCvSharp.Rect(0, cy, cx, cy));
            Mat q2 = new(magMat, new OpenCvSharp.Rect(cx, cy, cx, cy));
            Mat q3 = new(magMat, new OpenCvSharp.Rect(cx, 0, cx, cy));

            Mat tmp = new();
            q0.CopyTo(tmp);
            q2.CopyTo(q0);
            tmp.CopyTo(q2);

            q1.CopyTo(tmp);
            q3.CopyTo(q1);
            tmp.CopyTo(q3);

            //MatType, then to[0,255]
            Cv2.Normalize(magMat, magMat, 0, 1, NormTypes.MinMax);
            Mat magImg = new(magMat.Size(), MatType.CV_8UC1);
            magMat.ConvertTo(magImg, MatType.CV_8UC1, 255, 0);
            //Cv2.ImShow("test", magImg);

            //Turn into binary image
            Mat magThresh = new();
            Cv2.Threshold(magImg, magThresh, 150, 255, ThresholdTypes.Binary);
            //Cv2.ImShow("Threshold", magImg);

            //Find lines with Hough Transformation

            //Mat linImg = new(magImg.Size(), MatType.CV_8UC3);
            var lines = Cv2.HoughLines(magThresh, 1, Cv2.PI / 180, 100, 0, 0);
            int numLines = lines.Length;
            //for (int l = 0; l < numLines; l++)
            //{
            //    float rho = lines[l].Rho, theta = lines[l].Theta;
            //    Point pt1, pt2;
            //    double a = Math.Cos(theta), b = Math.Sin(theta);
            //    double x0 = a * rho, y0 = b * rho;
            //    pt1.X = (int)Math.Round(x0 + 1000 * (-b));
            //    pt1.Y = (int)Math.Round(y0 + 1000 * (a));
            //    pt2.X = (int)Math.Round(x0 - 1000 * (-b));
            //    pt2.Y = (int)Math.Round(y0 - 1000 * (a));
            //    Cv2.Line(linImg, pt1.X, pt1.Y, pt2.X, pt2.Y, Scalar.Blue);
            //}

            //从三个角度中找到真正的角度
            double angel = 0;
            var piThresh = Cv2.PI / 90;
            float pi2 = (float)Cv2.PI / 2;
            for (int l = 0; l < numLines; l++)
            {
                float theta = lines[l].Theta;
                if (Math.Abs(theta) < piThresh || Math.Abs(theta - pi2) < piThresh)
                    continue;
                else
                {
                    angel = theta;
                    break;
                }
            }
            //计算旋转角度
            //图像必须是正方形，
            //使旋转角度可以计算右
            angel = angel < pi2 ? angel : angel - (float)Cv2.PI;
            if (angel != pi2)
            {
                double angelT = src.Rows * Math.Tan(angel) / src.Cols;
                angel = Math.Atan(angelT);
            }
            double angelD = angel * 180 / Cv2.PI;
            DFT = magImg;
            return angelD;
        }


        /// <summary>
        /// 获取中心点位
        /// </summary>
        /// <param name="src">源图像</param>
        /// <returns>返回结果</returns>
        public static Point GetCenterPoint(this Mat src) => new(src.Width / 2, src.Height / 2);


        /// <summary>
        /// 获取HSVInRange
        /// </summary>
        /// <param name="src">源图像</param>
        /// <param name="hLow">hLow</param>
        /// <param name="sLow">sLow</param>
        /// <param name="vLow">vLow</param>
        /// <param name="hHigh">hHigh</param>
        /// <param name="sHigh">sHigh</param>
        /// <param name="vHigh">vHigh</param>
        /// <param name="dst">目标图像</param>
        /// <returns>返回布尔值</returns>
        public static bool GetHSVInRange(this Mat src,
            int hLow, int sLow, int vLow,
            int hHigh, int sHigh, int vHigh, out Mat dst)
        {
            dst = new Mat();
            if (src.Empty()) return false;

            if (src.Type() != MatType.CV_8UC3) return false;

            Mat hsv = new Mat();
            Cv2.CvtColor(src, hsv, ColorConversionCodes.BGR2HSV);
            Scalar low = new(hLow, sLow, vLow);
            Scalar high = new(hHigh, sHigh, vHigh);
            Cv2.InRange(hsv, low, high, dst);
            hsv.Dispose();
            return true;

        }


        //检查三点是否共线
        /// <summary>
        /// IsCollinear
        /// </summary>
        /// <param name="a">a</param>
        /// <param name="b">b</param>
        /// <param name="c">c</param>
        /// <returns>返回布尔值</returns>
        public static bool IsCollinear(Point2f a, Point2f b, Point2f c)
        {
            return Math.Abs((b.Y - a.Y) * (c.X - b.X) - (c.Y - b.Y) * (b.X - a.X)) < 1e-6;
        }
        //三点拟合圆
        /// <summary>
        /// Fit圆弧FromThreePoints
        /// </summary>
        /// <param name="p1">p1</param>
        /// <param name="p2">p2</param>
        /// <param name="p3">p3</param>
        /// <returns>返回结果</returns>
        public static CircleSegment FitCircleFromThreePoints(Point2f p1, Point2f p2, Point2f p3)
        {
            // 检查三点是否共线
            if (IsCollinear(p1, p2, p3))
                throw new ArgumentException("三点共线，无法确定唯一圆");

            // 计算分子和分母
            float denominator = 2 * (p1.X * (p2.Y - p3.Y) - p1.Y * (p2.X - p3.X) + p2.X * p3.Y - p3.X * p2.Y);

            float hNumerator = (p1.X * p1.X + p1.Y * p1.Y) * (p2.Y - p3.Y)
                             + (p2.X * p2.X + p2.Y * p2.Y) * (p3.Y - p1.Y)
                             + (p3.X * p3.X + p3.Y * p3.Y) * (p1.Y - p2.Y);

            float kNumerator = (p1.X * p1.X + p1.Y * p1.Y) * (p3.X - p2.X)
                             + (p2.X * p2.X + p2.Y * p2.Y) * (p1.X - p3.X)
                             + (p3.X * p3.X + p3.Y * p3.Y) * (p2.X - p1.X);

            Point2f center = new(
                hNumerator / denominator,
                kNumerator / denominator
            );

            float radius = (float)Math.Sqrt(Math.Pow(p1.X - center.X, 2) + Math.Pow(p1.Y - center.Y, 2));

            return new CircleSegment(center, radius);
        }

        /// <summary>
        /// 序列化仿射变换矩阵（2×3 Mat）为 JSON
        /// </summary>
        public static string SerializeAffineMat(Mat mat)
        {
            var matrix = new double[6];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    matrix[i * 3 + j] = mat.At<double>(i, j);
            return System.Text.Json.JsonSerializer.Serialize(new { type = "affine", matrix });
        }

        /// <summary>
        /// 反序列化 JSON 为仿射变换矩阵（2×3 Mat）
        /// </summary>
        public static Mat DeserializeAffineMat(string json)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var arr = doc.RootElement.GetProperty("matrix");
            var mat = new Mat(2, 3, MatType.CV_64F);
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    mat.Set(i, j, arr[i * 3 + j].GetDouble());
            return mat;
        }


    }

 
}