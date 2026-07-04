using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;

namespace PLCSharp.VVMs.Homepage
{
    /// <summary>
    /// 画布配置类
    /// </summary>
    public class CanvasConfig : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 配方标识
        /// </summary>
        public Guid RecipeID { get; set; }


        private int _Rows;
        /// <summary>
        /// 画布名称
        /// </summary>
        public int Rows
        {
            get { return _Rows; }
            set { SetProperty(ref _Rows, value); }
        }

        private int _Columns;
        /// <summary>
        /// 画布名称
        /// </summary>
        public int Columns
        {
            get { return _Columns; }
            set { SetProperty(ref _Columns, value); }
        }

    }
}
