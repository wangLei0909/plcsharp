using System.Windows.Media.Imaging;

namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// 文件Tools
    /// </summary>
    public class FileTools
    {



        /// <summary>
        /// 获取图像
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <returns>返回结果</returns>
        public static BitmapImage GetImage(string fileName)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(fileName);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

    }
}