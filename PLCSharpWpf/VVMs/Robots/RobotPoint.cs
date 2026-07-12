using Newtonsoft.Json;
using Prism.Mvvm;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLCSharp.VVMs.Robots
{
    /// <summary>
    /// 机器人点位
    /// </summary>
    public class RobotPoint : BindableBase
    {
        #region 属性
        /// <summary>
        /// 唯一标识
        /// </summary>
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 配方标识
        /// </summary>
        public Guid RecipeID { get; set; }
        /// <summary>
        /// 机器人ID
        /// </summary>
        public Guid RobotID { get; set; }

        /// <summary>
        /// 点位类型 
        /// </summary>
        public int PointType { get; set; }

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
            set
            {
                value = Math.Round(value, 4);
                SetProperty(ref _X, value);
            }
        }

        private double _Y;
        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y
        {
            get { return _Y; }
            set
            {
                value = Math.Round(value, 4);
                SetProperty(ref _Y, value);
            }
        }

        private double _Z;
        /// <summary>
        /// Z坐标
        /// </summary>
        public double Z
        {
            get { return _Z; }
            set
            {
                value = Math.Round(value, 4);
                SetProperty(ref _Z, value);
            }
        }

        private double _U;
        /// <summary>
        /// U坐标 (Rx)
        /// </summary>
        public double U
        {
            get { return _U; }
            set
            {
                value = Math.Round(value, 4);
                SetProperty(ref _U, value);
            }
        }

        private double _V;
        /// <summary>
        /// V
        /// </summary>
        public double V
        {
            get { return _V; }
            set
            {
                value = Math.Round(value, 4);
                SetProperty(ref _V, value);
            }
        }


        private double _W;
        /// <summary>
        /// W
        /// </summary>
        public double W
        {
            get { return _W; }
            set
            {
                value = Math.Round(value, 4);
                SetProperty(ref _W, value);
            }
        }


        private double _Rate = 100;
        /// <summary>
        /// 速率 1-100
        /// </summary>
        public double Rate
        {
            get { return _Rate; }
            set
            {
                if (value > 100)
                    value = 100;
                else if (value < 1)
                    value = 1;
                SetProperty(ref _Rate, value);
            }
        }

        #region 左右手
        private HandType _Hand = HandType.R;
        /// <summary>
        /// 左右手 0-L 1-R
        /// </summary>
        public HandType Hand
        {
            get { return _Hand; }
            set { SetProperty(ref _Hand, value); }
        }
        #endregion

        #region 工具坐标系
        private int _ToolNum = 0;
        /// <summary>
        /// 工具坐标系编号
        /// </summary>
        public int ToolNum
        {
            get { return _ToolNum; }
            set { SetProperty(ref _ToolNum, value); }
        }
        #endregion

        #region 用户坐标系
        private short _UF = 0;
        /// <summary>
        /// 用户坐标系
        /// </summary>
        public short UF
        {
            get { return _UF; }
            set { SetProperty(ref _UF, value); }
        }
        #endregion

        /// <summary>
        /// Safe
        /// </summary>
        [NotMapped]
        /// <summary>
        /// 是否安全可运行
        /// </summary>
        public bool Safe { get; set; } = true;

        /// <summary>
        /// 序列化Params
        /// </summary>
        [Column("Params")]
        public string SerializedParams
        {
            get => JsonConvert.SerializeObject(Params);
            set => Params = JsonConvert.DeserializeObject<PointParams>(value);
        }

        private PointParams _Params;
        /// <summary>
        /// 参数集合
        /// </summary>
        [NotMapped]
        public PointParams Params
        {
            get
            {
                _Params ??= new PointParams();
                return _Params;
            }
            set
            {
                SetProperty(ref _Params, value);
            }
        }
        /// <summary>
        /// Command
        /// </summary>
        [NotMapped]
        public string Command { get; set; }

        /// <summary>
        /// 在矩阵中的X索引
        /// </summary>
        [NotMapped]
        public int XIndex { get; internal set; }
        /// <summary>
        /// 在矩阵中的Y索引
        /// </summary>
        [NotMapped]
        public int YIndex { get; internal set; }

        /// <summary>
        /// 点位Params
        /// </summary>
        public class PointParams : BindableBase
        {

        }
        #endregion

    }
}
