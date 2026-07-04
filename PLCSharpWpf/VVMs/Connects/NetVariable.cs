namespace PLCSharp.VVMs.Connects
{
    /// <summary>
    /// 网络变量
    /// </summary>
    /// <typeparam name="T">变量类型</typeparam>
    public class NetVariable
    {
        /// <summary>
        /// IsSucceed
        /// </summary>
        public bool IsSucceed { get; set; }

        /// <summary>
        /// ErrCode
        /// </summary>
        public int ErrCode { get; set; }

        /// <summary>
        /// Err信息
        /// </summary>
        public string ErrInfo { get; set; }

        /// <summary>
        /// Address
        /// </summary>
        public string Address { get; set; }


        /// <summary>
        /// 类型
        /// </summary>
        public Type Type { get; set; }
        /// <summary>
        /// 值
        /// </summary>
        public dynamic Value { get; set; }
        /// <summary>
        /// 请求报文
        /// </summary>
        public string Requst { get; set; }

        /// <summary>
        /// 响应报文
        /// </summary>
        public string Response { get; set; }



    }
}
