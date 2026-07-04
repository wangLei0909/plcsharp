using System.Text.RegularExpressions;

namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// StringExtension
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// IsIP
        /// </summary>
        /// <param name="ip">ip</param>
        /// <returns>返回布尔值</returns>
        public static bool IsIP(this string ip)
        {
            return Regex.IsMatch(ip, @"^([1-9]\d?|1\d{2}|2[01]\d|22[0-3])(\.([1-9]?\d|1\d{2}|2[0-4]\d|25[0-5])){3}$");
        }
    }
}