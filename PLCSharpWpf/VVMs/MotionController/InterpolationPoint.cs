using Prism.Mvvm;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// 插补点位
    /// </summary>
    public class InterpolationPoint : BindableBase
    {
        #region 属性
        private string _Name;
        /// <summary>
        /// 点名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private double _X;
        /// <summary>
        /// X坐标
        /// </summary>
        public double X
        {
            get { return _X; }
            set { SetProperty(ref _X, value); }
        }

        private double _Y;
        /// <summary>
        /// Y
        /// </summary>
        public double Y
        {
            get { return _Y; }
            set { SetProperty(ref _Y, value); }
        }
        #endregion


    }
}
