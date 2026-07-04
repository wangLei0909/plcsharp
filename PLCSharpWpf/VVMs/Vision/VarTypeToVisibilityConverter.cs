using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// 将 VarType 字符串与 ConverterParameter 比较，相等则 Visible 否则 Collapsed
    /// </summary>
    public class VarTypeToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 将 VarType 与参数比较，决定是否显示对应的编辑面板
        /// </summary>
        /// <param name="value">变量的 VarType 值</param>
        /// <param name="targetType">目标类型（未使用）</param>
        /// <param name="parameter">要匹配的类型名称</param>
        /// <param name="culture">区域化信息（未使用）</param>
        /// <returns>匹配时返回 Visible，否则返回 Collapsed</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string varType = value as string ?? "";
            string target = parameter as string ?? "";
            return varType == target ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 反向转换（未实现，仅用于满足接口要求）
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">区域化信息</param>
        /// <returns>不适用，始终抛出异常</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
