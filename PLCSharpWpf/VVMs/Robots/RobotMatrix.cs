using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLCSharp.VVMs.Robots
{
    /// <summary>
    /// 机器人矩阵（平行四边形插值生成点位网格）
    /// </summary>
    public class RobotMatrix : BindableBase
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
        /// <summary>
        /// 所属机器人ID
        /// </summary>
        public Guid RobotID { get; set; }

        private string _Name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }

        private string _StartName;
        /// <summary>
        /// 首点名（原点 (0,0)）
        /// </summary>
        public string StartName
        {
            get { return _StartName; }
            set { SetProperty(ref _StartName, value); }
        }

        private string _XEndName;
        /// <summary>
        /// X方向尾点名（(XCount-1, 0)）
        /// </summary>
        public string XEndName
        {
            get { return _XEndName; }
            set { SetProperty(ref _XEndName, value); }
        }

        private string _YEndName;
        /// <summary>
        /// Y方向尾点名（(0, YCount-1)）
        /// </summary>
        public string YEndName
        {
            get { return _YEndName; }
            set { SetProperty(ref _YEndName, value); }
        }

        private int _XCount;
        /// <summary>
        /// X方向点数
        /// </summary>
        public int XCount
        {
            get { return _XCount; }
            set { SetProperty(ref _XCount, value); }
        }

        private int _YCount;
        /// <summary>
        /// Y方向点数
        /// </summary>
        public int YCount
        {
            get { return _YCount; }
            set { SetProperty(ref _YCount, value); }
        }

        private int _MatrixType;
        /// <summary>
        /// 矩阵元素顺序 0先X后Y, 1先Y后X
        /// </summary>
        public int MatrixType
        {
            get { return _MatrixType; }
            set { SetProperty(ref _MatrixType, value); }
        }

        private int _XTarget;
        /// <summary>
        /// 手动运行时输入的X目标
        /// </summary>
        [NotMapped]
        public int XTarget
        {
            get { return _XTarget; }
            set { SetProperty(ref _XTarget, value); }
        }

        private int _YTarget;
        /// <summary>
        /// 手动运行时输入的Y目标
        /// </summary>
        [NotMapped]
        public int YTarget
        {
            get { return _YTarget; }
            set { SetProperty(ref _YTarget, value); }
        }

        /// <summary>
        /// 生成的点位列表
        /// </summary>
        [NotMapped]
        public List<RobotPoint> Points { get; set; } = [];

        /// <summary>
        /// 按索引查找点位
        /// </summary>
        public RobotPoint? GetPoint(int x, int y)
        {
            return Points.FirstOrDefault(p => p.XIndex == x && p.YIndex == y);
        }
    }
}
