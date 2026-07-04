using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// 阈值Handler
    /// </summary>
    public class ThresholdHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.阈值;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="func">视觉功能</param>
        /// <param name="item">变量项</param>
        /// <returns>返回布尔值</returns>
        public bool Execute(VisionFunction func, VisionFlow item)
        {

            if (item.IntParams.TryGetValue("ThresholdType", out int thresholdType))
            {
                if (item.IntParams.TryGetValue("Threshold", out int threshold))
                {
                    if (item.IntParams.TryGetValue("MaxValue", out int maxValue))
                    {
                        if (item.BoolParams.TryGetValue("IsOtsu", out bool isOtsu))
                        {

                            if (item.BoolParams.TryGetValue("IsTriangle", out bool isTriangle))
                            {

                                if (func.Src.Channels() == 3)
                                    Cv2.CvtColor(func.Src, func.Src, ColorConversionCodes.BGR2GRAY);


                                ThresholdTypes type = (ThresholdTypes)thresholdType;

                                if (isOtsu)
                                {
                                    type |= ThresholdTypes.Otsu;
                                    item.BoolParams["IsTriangle"] = false;
                                    isTriangle = false;
                                }
                                if (isTriangle)
                                {
                                    type |= ThresholdTypes.Triangle;

                                }
                                if (maxValue < threshold) maxValue = threshold + 1;

                                Cv2.Threshold(func.Src, func.Src, threshold, maxValue, type);

                                return true;
                            }
                        }
                    }

                }

            }
            return false;
        }
    }
}
