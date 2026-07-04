using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// BoolTo值Converter
    /// </summary>
    public class BoolToValueConverter : IValueConverter

    {

        #region IValueConverter Members



        /// <summary>
        /// 转换
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="targetType">target类型</param>
        /// <param name="parameter">parameter</param>
        /// <param name="culture">culture</param>
        /// <returns>返回 object</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)

        {

            if ((bool)value)

                return parameter;

            else

                return DependencyProperty.UnsetValue;

        }



        /// <summary>
        /// 转换Back
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="targetType">target类型</param>
        /// <param name="parameter">parameter</param>
        /// <param name="culture">culture</param>
        /// <returns>返回 object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)

        {

            return Equals(value, parameter);

        }



        #endregion

    }
}
