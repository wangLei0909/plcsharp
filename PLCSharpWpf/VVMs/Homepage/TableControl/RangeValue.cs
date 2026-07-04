using Prism.Mvvm;

namespace PLCSharp.VVMs.Homepage.TableControl
{
    /// <summary>
    /// Range值
    /// </summary>
    public class RangeValue : BindableBase
    {
        private string _Name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }


        private double _MaxValue;
        /// <summary>
        /// 范围值
        /// </summary>
        public double MaxValue
        {
            get { return _MaxValue; }
            set { SetProperty(ref _MaxValue, value); }
        }


        private double _MinValue;
        /// <summary>
        /// 范围值
        /// </summary>
        public double MinValue
        {
            get { return _MinValue; }
            set { SetProperty(ref _MinValue, value); }
        }

        private double _TestValue;
        /// <summary>
        /// 测量值
        /// </summary>
        public double TestValue
        {
            get { return _TestValue; }
            set { SetProperty(ref _TestValue, value); }
        }


        private double _FixValue;
        /// <summary>
        /// 修正值
        /// </summary>
        public double FixValue
        {
            get { return _FixValue; }
            set { SetProperty(ref _FixValue, value); }
        }

        private double _ShowValue;
        /// <summary>
        /// 显示值
        /// </summary>
        public double ShowValue
        {
            get { return _ShowValue; }
            set { SetProperty(ref _ShowValue, value); }
        }

        private double _Offs;
        /// <summary>
        /// 偏差
        /// </summary>
        public double Offs
        {
            get { return _Offs; }
            set { SetProperty(ref _Offs, value); }
        }
        private int _Result;
        /// <summary>
        /// 结果 0 未测 1 OK  2 NG
        /// </summary>
        public int Result
        {
            get { return _Result; }
            set
            {
                SetProperty(ref _Result, value);
                ResultStr = value switch
                {
                    1 => "OK",
                    2 => "NG",
                    _ => "",
                };
            }
        }

        private string _ResultStr;
        /// <summary>
        /// 范围值
        /// </summary>
        public string ResultStr
        {
            get { return _ResultStr; }
            set { SetProperty(ref _ResultStr, value); }
        }

        private double _BaseValue;
        /// <summary>
        /// 标准值
        /// </summary>
        public double BaseValue
        {
            get { return _BaseValue; }
            set { SetProperty(ref _BaseValue, value); }
        }
        /// <summary>
        /// 计算结果
        /// </summary>
        public void GetResult()
        {
            Offs = ShowValue - BaseValue;
            Result = ShowValue >= MinValue && ShowValue <= MaxValue ? 1 : 2;
        }
    }
}
