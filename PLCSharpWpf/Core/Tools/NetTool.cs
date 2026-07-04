using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// Net工具
    /// </summary>
    public static class NetTool
    {
        private static readonly Ping Ping = new();
        /// <summary>
        /// PingIPAsync
        /// </summary>
        /// <param name="ip">ip</param>
        /// <returns>返回结果</returns>
        public static async Task<bool> PingIPAsync(string ip)
        {
            var isip = IPAddress.TryParse(ip, out var ipAddress);
            if (isip == false)
            {
                return false;
            }
            bool isMatch = Regex.IsMatch(ip, @"^([1-9]\d?|1\d{2}|2[01]\d|22[0-3])(\.([1-9]?\d|1\d{2}|2[0-4]\d|25[0-5])){3}$");
            if (isMatch == false)
            {
                return false;
            }
            try
            {
                var result = await Ping.SendPingAsync(ipAddress);
                return result.Status == IPStatus.Success;
            }
            catch
            {

                return false;
            }
        }


        /// <summary>
        /// PingIP
        /// </summary>
        /// <param name="ip">ip</param>
        /// <returns>返回布尔值</returns>
        public static bool PingIP(string ip)
        {
            lock (Ping)
            {


                var isip = IPAddress.TryParse(ip, out var ipAddress);
                if (isip == false)
                {
                    return false;
                }
                bool isMatch = Regex.IsMatch(ip, @"^([1-9]\d?|1\d{2}|2[01]\d|22[0-3])(\.([1-9]?\d|1\d{2}|2[0-4]\d|25[0-5])){3}$");
                if (isMatch == false)
                {
                    return false;
                }
                try
                {

                    var result = Ping.Send(ipAddress);
                    return result.Status == IPStatus.Success;
                }
                catch
                {

                    return false;
                }
            }
        }
    }
}
