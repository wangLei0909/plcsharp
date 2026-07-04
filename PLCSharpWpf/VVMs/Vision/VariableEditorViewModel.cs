using OpenCvSharp;
using PLCSharp.Core.Prism;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// 局部变量表编辑对话框的 ViewModel
    /// </summary>
    public class VariableEditorViewModel : DialogAwareBase
    {
        /// <summary>
        /// 构造编辑对话框的视图模型
        /// </summary>
        public VariableEditorViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService)
            : base(container, ea, dialogService)
        {
        }

        private LocalVariableItem _Item;
        /// <summary>
        /// 正在编辑的变量项
        /// </summary>
        public LocalVariableItem Item
        {
            get { return _Item; }
            set { SetProperty(ref _Item, value); }
        }

        private string _VarType;
        /// <summary>
        /// 变量类型：Point / Line / Circle / Rect / Double
        /// </summary>
        public string VarType
        {
            get { return _VarType; }
            set { SetProperty(ref _VarType, value); }
        }

        private string _VarName;
        /// <summary>
        /// 变量名称
        /// </summary>
        public string VarName
        {
            get { return _VarName; }
            set { SetProperty(ref _VarName, value); }
        }

        // ---- Point ----
        private double _PointX;
        /// <summary>
        /// 点 X 坐标
        /// </summary>
        public double PointX { get => _PointX; set => SetProperty(ref _PointX, value); }
        private double _PointY;
        /// <summary>
        /// 点 Y 坐标
        /// </summary>
        public double PointY { get => _PointY; set => SetProperty(ref _PointY, value); }

        // ---- Line ----
        private double _LineVx, _LineVy, _LineX1, _LineY1;
        /// <summary>
        /// 直线方向向量的 X 分量
        /// </summary>
        public double LineVx { get => _LineVx; set => SetProperty(ref _LineVx, value); }
        /// <summary>
        /// 直线方向向量的 Y 分量
        /// </summary>
        public double LineVy { get => _LineVy; set => SetProperty(ref _LineVy, value); }
        /// <summary>
        /// 直线上一点的 X 坐标
        /// </summary>
        public double LineX1 { get => _LineX1; set => SetProperty(ref _LineX1, value); }
        /// <summary>
        /// 直线上一点的 Y 坐标
        /// </summary>
        public double LineY1 { get => _LineY1; set => SetProperty(ref _LineY1, value); }

        // ---- Circle ----
        private double _CircleCx, _CircleCy, _CircleR;
        /// <summary>
        /// 圆心 X
        /// </summary>
        public double CircleCx { get => _CircleCx; set => SetProperty(ref _CircleCx, value); }
        /// <summary>
        /// 圆心 Y
        /// </summary>
        public double CircleCy { get => _CircleCy; set => SetProperty(ref _CircleCy, value); }
        /// <summary>
        /// 圆半径
        /// </summary>
        public double CircleR { get => _CircleR; set => SetProperty(ref _CircleR, value); }

        // ---- Rect ----
        private double _RectX, _RectY, _RectW, _RectH;
        /// <summary>
        /// 矩形左上角 X
        /// </summary>
        public double RectX { get => _RectX; set => SetProperty(ref _RectX, value); }
        /// <summary>
        /// 矩形左上角 Y
        /// </summary>
        public double RectY { get => _RectY; set => SetProperty(ref _RectY, value); }
        /// <summary>
        /// 矩形宽度
        /// </summary>
        public double RectW { get => _RectW; set => SetProperty(ref _RectW, value); }
        /// <summary>
        /// 矩形高度
        /// </summary>
        public double RectH { get => _RectH; set => SetProperty(ref _RectH, value); }

        // ---- Double ----
        private double _DoubleVal;
        /// <summary>
        /// 数值
        /// </summary>
        public double DoubleVal { get => _DoubleVal; set => SetProperty(ref _DoubleVal, value); }

        private DelegateCommand _ConfirmCommand;
        /// <summary>
        /// 确认编辑并关闭对话框
        /// </summary>
        public DelegateCommand ConfirmCommand =>
            _ConfirmCommand ??= new DelegateCommand(ExecuteConfirm);

        void ExecuteConfirm()
        {
            if (_Item == null) return;

            // 更新 Item 名称
            _Item.Name = _VarName;

            // 根据类型构建 typed 值
            switch (_VarType)
            {
                case "Pos":
                    _Item.RawValue =  new Pos(_PointX, _PointY, 0, 0) ;
                    break;
                case "Line":
                    _Item.RawValue = new Line(new Pos(_LineVx, _LineVy, 0, 0), new Pos(_LineX1, _LineY1, 0, 0)) ;
                    break;
                case "Circle":
                    _Item.RawValue =  new Circle(new Pos(_CircleCx, _CircleCy, 0, 0), _CircleR) ;
                    break;
                case "Rect":
                    _Item.RawValue =  new Rect(new Pos(_RectX, _RectY, 0, 0), _RectW, _RectH) ;
                    break;
                case "Double":
                    _Item.RawValue = (_DoubleVal);
                    break;
            }

            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        /// <summary>
        /// 打开对话框后执行初始化，根据变量类型加载对应的编辑字段
        /// </summary>
        /// <param name="parameters">传入的参数列表，需包含键为 "item" 的 LocalVariableItem</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            _Item = parameters.GetValue<LocalVariableItem>("item");
            if (_Item == null)
            {
                RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
                return;
            }

            Title = "编辑变量 - " + _Item.Name;
            VarType = _Item.VarType;
            VarName = _Item.Name;

            // 从 RawValue 加载各字段；若 RawValue 为空则从 StrValue 尝试
            var raw = _Item.RawValue;
            if (raw == null)
            {
                // 无法编辑无原始值的项目
                Message = "该变量无原始数据，无法编辑";
                return;
            }

            switch (_VarType)
            {
                case "Pos":
                    if (raw is Pos pt)
                    {
                        PointX = pt.X;
                        PointY = pt.Y;
                    }
                    break;
                case "Line":
                    if (raw is Line  line)
                    {
                        LineVx = line.From.X;
                        LineVy = line.From.Y;
                        LineX1 = line.To.X;
                        LineY1 = line.To.Y;
                    }
                    break;
                case "Circle":
                    if (raw is Circle  cs)
                    {
                        CircleCx = cs.Center.X;
                        CircleCy = cs.Center.Y;
                        CircleR = cs.Radius;
                    }
                    break;
                case "Rect":
                    if (raw is Rect r)
                    {
                        RectX = r.Center.X;
                        RectY = r.Center.Y;
                        RectW = r.Width;
                        RectH = r.Height;
                    }
                    break;
                case "Double":
                    DoubleVal = raw is double d ? d : 0;
                    break;
            }
        }
    }
}
