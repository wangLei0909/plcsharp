using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// 图像旋转Handler — 按指定角度旋转图像，可选择保持原尺寸或扩展画布
    /// </summary>
    public class ImageRotateHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.图像旋转;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src ?? throw new Exception("请先获取图片！");

            // 读取旋转角度
            double angle = item.DoubleParams.TryGetValue("RotateAngle", out double a) ? a : 0;

            // 读取尺寸模式：0=保持原尺寸，1=扩展画布（完整显示）
            int resizeMode = item.IntParams.TryGetValue("ResizeMode", out int rm) ? rm : 0;

            int w = src.Width, h = src.Height;
            var center = new Point2f(w / 2f, h / 2f);

            var rot = Cv2.GetRotationMatrix2D(center, angle, 1.0);

            Size dstSize;
            if (resizeMode == 1)
            {
                // 计算旋转后最小外接矩形尺寸
                double rad = angle * Math.PI / 180.0;
                double cos = Math.Abs(Math.Cos(rad));
                double sin = Math.Abs(Math.Sin(rad));
                int newW = (int)(w * cos + h * sin);
                int newH = (int)(w * sin + h * cos);
                dstSize = new Size(newW, newH);

                // 调整平移量使图像居中
                rot.Set<double>(0, 2, rot.Get<double>(0, 2) + (newW - w) / 2.0);
                rot.Set<double>(1, 2, rot.Get<double>(1, 2) + (newH - h) / 2.0);
            }
            else
            {
                dstSize = new Size(w, h);
            }

            using Mat dst = new Mat();
            Cv2.WarpAffine(src, dst, rot, dstSize);

            dst.CopyTo(src);
            return true;
        }
    }
}
