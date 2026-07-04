namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// Bit工具
    /// </summary>
    public static class BitTool
    {

        #region uint

        /// <summary>
        /// ToBitArray
        /// </summary>
        /// <param name="intValue">int值</param>
        /// <returns>返回结果</returns>
        public static bool[] ToBitArray(this uint intValue)
        {
            bool[] bitArray = new bool[32];
            for (var i = 0; i <= 31; i++)
            {
                var val = 1 << i;
                bitArray[i] = (intValue & val) == val;
            }
            return bitArray;
        }

        /// <summary>
        /// 获取Bit
        /// </summary>
        /// <param name="intValue">int值</param>
        /// <param name="index">当前索引</param>
        /// <returns>返回布尔值</returns>
        public static bool GetBit(this uint intValue, int index)
        {
            if (index > 31 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;
            return (intValue & val) == val;
        }

        /// <summary>
        /// 设置Bit
        /// </summary>
        /// <param name="intValue">int值</param>
        /// <param name="index">当前索引</param>
        public static void SetBit(ref this uint intValue, int index)
        {
            if (index > 31 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;

            intValue |= (uint)val;
        }

        /// <summary>
        /// 重置Bit
        /// </summary>
        /// <param name="intValue">int值</param>
        /// <param name="index">当前索引</param>
        public static void ResetBit(ref this uint intValue, int index)
        {
            if (index > 31 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;

            intValue &= ~(uint)val;
        }

        #endregion  

        #region int

        /// <summary>
        /// ToBitArray
        /// </summary>
        /// <param name="intValue">int值</param>
        /// <returns>返回结果</returns>
        public static bool[] ToBitArray(this int intValue)
        {
            bool[] bitArray = new bool[32];
            for (var i = 0; i <= 31; i++)
            {
                var val = 1 << i;
                bitArray[i] = (intValue & val) == val;
            }
            return bitArray;
        }

        /// <summary>
        /// 获取Bit
        /// </summary>
        /// <param name="intValue">int值</param>
        /// <param name="index">当前索引</param>
        /// <returns>返回布尔值</returns>
        public static bool GetBit(this int intValue, int index)
        {
            if (index > 31 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;
            return (intValue & val) == val;
        }

        /// <summary>
        /// 设置Bit
        /// </summary>
        /// <param name="intValue">int值</param>
        /// <param name="index">当前索引</param>
        public static void SetBit(ref this int intValue, int index)
        {
            if (index > 31 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;

            intValue |= val;
        }

        /// <summary>
        /// 重置Bit
        /// </summary>
        /// <param name="intValue">int值</param>
        /// <param name="index">当前索引</param>
        public static void ResetBit(ref this int intValue, int index)
        {
            if (index > 31 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;

            intValue &= ~val;
        }

        #endregion  

        #region byte

        /// <summary>
        /// ToBitArray
        /// </summary>
        /// <param name="byteValue">byte值</param>
        /// <returns>返回结果</returns>
        public static bool[] ToBitArray(this byte byteValue)
        {
            bool[] bitArray = new bool[8];
            for (var i = 0; i < 8; i++)
            {
                var val = 1 << i;
                bitArray[i] = (byteValue & val) == val;
            }
            return bitArray;
        }

        /// <summary>
        /// Bit
        /// </summary>
        /// <param name="byteValue">byte值</param>
        /// <param name="index">当前索引</param>
        /// <param name="tf">tf</param>
        public static void Bit(ref this byte byteValue, int index, bool tf)
        {
            if (index >= 8 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;
            if (tf) byteValue |= (byte)val; else byteValue &= (byte)~val;
        }

        /// <summary>
        /// 获取Bit
        /// </summary>
        /// <param name="byteValue">byte值</param>
        /// <param name="index">当前索引</param>
        /// <returns>返回布尔值</returns>
        public static bool GetBit(this byte byteValue, int index)
        {
            if (index >= 8 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;
            return (byteValue & val) == val;
        }

        /// <summary>
        /// 设置Bit
        /// </summary>
        /// <param name="byteValue">byte值</param>
        /// <param name="index">当前索引</param>
        public static void SetBit(ref this byte byteValue, int index)
        {
            if (index >= 8 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;

            byteValue |= (byte)val;
        }

        /// <summary>
        /// ResteBit
        /// </summary>
        /// <param name="byteValue">byte值</param>
        /// <param name="index">当前索引</param>
        public static void ResteBit(ref this byte byteValue, int index)
        {
            if (index >= 8 || index < 0) throw new Exception("索引越界");

            var val = 1 << index;

            byteValue &= (byte)~val;
        }

        #endregion

    }
}