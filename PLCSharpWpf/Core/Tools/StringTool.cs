using System.Text;

namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// String工具
    /// </summary>
    internal static class StringTool
    {
        private static readonly char[] constant =
        [
        '0','1','2','3','4','5','6','7','8','9',
        'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
        'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
        ];

        /// <summary>
        /// GenerateRandomNumber
        /// </summary>
        /// <param name="Length">Length</param>
        /// <returns>返回字符串</returns>
        public static string GenerateRandomNumber(int Length)
        {
            StringBuilder newRandom = new(62);
            Random rd = new();
            for (int i = 0; i < Length; i++)
            {
                newRandom.Append(constant[rd.Next(62)]);
            }
            return newRandom.ToString();
        }
    }
}
