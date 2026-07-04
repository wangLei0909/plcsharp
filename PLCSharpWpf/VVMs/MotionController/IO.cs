using MiniExcelLibs.Attributes;
using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// DI
    /// </summary>
    public class DI : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        [ExcelIgnore]
        public Guid ID { get; set; } = Guid.NewGuid();
        /// <summary>
        /// ControllerID
        /// </summary>
        [ExcelIgnore]
        public Guid ControllerID { get; set; }
        private ushort _ControllerNumber;
        /// <summary>
        /// ControllerNo
        /// </summary>
        [NotMapped]
        [ExcelIgnore]
        /// <summary>
        /// 所属的控制序号
        /// </summary>
        public ushort ControllerNo
        {
            get { return _ControllerNumber; }
            set { SetProperty(ref _ControllerNumber, value); }
        }

        private string _Name;
        /// <summary>
        /// 点名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private ushort _Number;
        /// <summary>
        /// IO序号
        /// </summary>
        public ushort Number
        {
            get { return _Number; }
            set { SetProperty(ref _Number, value); }
        }

        private string _LineNumber;
        /// <summary>
        /// 线号
        /// </summary>
        public string LineNumber
        {
            get { return _LineNumber; }
            set { SetProperty(ref _LineNumber, value); }
        }

        private bool _Status;
        /// <summary>
        /// 状态
        /// </summary>
        [NotMapped]
        [ExcelIgnore]
        /// <summary>
        /// 状态
        /// </summary>
        public bool Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

    }
    /// <summary>
    /// DQ
    /// </summary>
    public class DQ : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        [ExcelIgnore]
        public Guid ID { get; set; } = Guid.NewGuid();
        /// <summary>
        /// ControllerID
        /// </summary>
        [ExcelIgnore]
        public Guid ControllerID { get; set; }

        private ushort _ControllerNumber;
        /// <summary>
        /// ControllerNo
        /// </summary>
        [NotMapped]
        [ExcelIgnore]
        /// <summary>
        /// 所属的控制序号
        /// </summary>
        public ushort ControllerNo
        {
            get { return _ControllerNumber; }
            set { SetProperty(ref _ControllerNumber, value); }
        }

        private string _Name;
        /// <summary>
        /// 点名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private ushort _Number;
        /// <summary>
        /// IO序号
        /// </summary>
        public ushort Number
        {
            get { return _Number; }
            set { SetProperty(ref _Number, value); }
        }

        private string _LineNumber;
        /// <summary>
        /// 线号
        /// </summary>
        public string LineNumber
        {
            get { return _LineNumber; }
            set { SetProperty(ref _LineNumber, value); }
        }

        private bool _Status;
        /// <summary>
        /// 状态
        /// </summary>
        [ExcelIgnore]
        [NotMapped]
        /// <summary>
        /// 状态
        /// </summary>
        public bool Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

    }
}
