namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// 标记 <see cref="VisionFlowType"/> 枚举值所属的菜单分类
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FlowCategoryAttribute(string category) : Attribute
    {
        /// <summary>
        /// 菜单分类名称（如"图像处理"、"存取图片"、"算法"）
        /// </summary>
        public string Category { get; } = category;
    }

    public enum VisionFlowType
    {
        [FlowCategory("存取图片")]
        拍照 = 0,
        [FlowCategory("存取图片")]
        从全局图像获取图片 = 1,
        [FlowCategory("存取图片")]
        从文件获取图片 = 2,
        [FlowCategory("存取图片")]
        从局部图像获取图片 = 3,

        [FlowCategory("存取图片")]
        存到全局图像 = 4,
        [FlowCategory("存取图片")]
        存到文件 = 5,
        [FlowCategory("存取图片")]
        存到局部图像 = 6,
        [FlowCategory("存取图片")]
        显示图像到主页 = 7,


        [FlowCategory("图像处理")]
        GRAY2BGR = 10,
        [FlowCategory("图像处理")]
        BGR2GRAY = 11,
        [FlowCategory("图像处理")]
        取通道 = 12,

        [FlowCategory("图像处理")]
        各通道最小值 = 15,
        [FlowCategory("图像处理")]
        各通道最大值 = 16,
        [FlowCategory("图像处理")]
        阈值 = 17,

        [FlowCategory("图像处理")]
        坐标转换 = 18,



        [FlowCategory("算法")]
        颜色面积 = 30,
        [FlowCategory("算法")]
        两线交点 = 31,
        [FlowCategory("算法")]
        卡尺寻边 = 32,
        [FlowCategory("算法")]
        ORB匹配 = 33,
        [FlowCategory("算法")]
        卡尺找圆 = 34,
        [FlowCategory("算法")]
        卡尺找旋转矩形 = 35,
        清除绘制 = 36,

        [FlowCategory("算法")]
        ROI解码 = 37,
        [FlowCategory("算法")]
        微信解码 = 39,
    }



}
