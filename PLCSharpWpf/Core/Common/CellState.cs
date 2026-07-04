using Prism.Mvvm;

namespace PLCSharp.Core.Common
{

    public enum CellState
    {
        无料 = 0,
        有料 = 1,
        允许 = 2,
        完成 = 3,
        请求 = 4,
        确认 = 5,
        OK = 100,
        NG = 101
    }

    /// <summary>
    /// Cell信息
    /// </summary>
    public class CellInfo : BindableBase
    {

        private int _Index;
        /// <summary>
        /// 单元格索引
        /// </summary>
        public int Index
        {
            get { return _Index; }
            set { SetProperty(ref _Index, value); }
        }

        private int _Row;
        /// <summary>
        /// 行号
        /// </summary>
        public int Row
        {
            get { return _Row; }
            set { SetProperty(ref _Row, value); }
        }

        private int _Column;
        /// <summary>
        /// 列号
        /// </summary>
        public int Column
        {
            get { return _Column; }
            set { SetProperty(ref _Column, value); }
        }

        private CellState _State;
        /// <summary>
        /// 状态标志
        /// </summary>
        public CellState State
        {
            get { return _State; }
            set
            {
                SetProperty(ref _State, value);

                _CellChanged?.Invoke(this, _State);
            }
        }

        private string _Info;
        /// <summary>
        /// 附加信息
        /// </summary>
        public string Info
        {
            get { return _Info; }
            set { SetProperty(ref _Info, value); }
        }

        private string _Tag;
        /// <summary>
        /// 标签
        /// </summary>
        public string Tag
        {
            get { return _Tag; }
            set { SetProperty(ref _Tag, value); }
        }

        public event EventHandler<CellState> _CellChanged;


    }
}
