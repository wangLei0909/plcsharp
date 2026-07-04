using PLCSharp.Core.Prism;
using PLCSharp.VVMs.Homepage.TableControl;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Windows;

namespace PLCSharp.VVMs.Homepage
{
    /// <summary>
    /// Custom控件配置视图模型
    /// </summary>
    public class CustomControlConfigViewModel : DialogAwareBase
    {
        private CustomControl _SelectedCustomControl;
        private RangeValue _SelectedRangeValue;

        /// <summary>
        /// Custom控件配置视图模型
        /// </summary>
        public CustomControlConfigViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
        }

        /// <summary>
        /// SelectedCustom控件
        /// </summary>
        public CustomControl SelectedCustomControl
        {
            get => _SelectedCustomControl;
            set
            {
                if (SetProperty(ref _SelectedCustomControl, value))
                {
                    RaisePropertyChanged(nameof(IsStateType));
                    RaisePropertyChanged(nameof(IsRangeValueTalbeType));
                }
            }
        }

        /// <summary>
        /// 选中的 RangeValue 行
        /// </summary>
        public RangeValue SelectedRangeValue
        {
            get => _SelectedRangeValue;
            set => SetProperty(ref _SelectedRangeValue, value);
        }

        /// <summary>
        /// 是否为 State 类型（显示布局配置）
        /// </summary>
        public bool IsStateType => SelectedCustomControl?.Type == ControlType.State;

        /// <summary>
        /// 是否为 RangeValueTalbe 类型（显示范围值编辑）
        /// </summary>
        public bool IsRangeValueTalbeType => SelectedCustomControl?.Type == ControlType.RangeValueTalbe;

        private DelegateCommand _AddRangeValueCommand;
        /// <summary>
        /// 新增 RangeValue 行
        /// </summary>
        public DelegateCommand AddRangeValueCommand =>
            _AddRangeValueCommand ??= new DelegateCommand(ExecuteAddRangeValue);

        void ExecuteAddRangeValue()
        {
            if (SelectedCustomControl == null) return;
            var rv = new RangeValue
            {
                Name = $"Item{SelectedCustomControl.RangeValues.Count + 1}",
                MinValue = 0,
                MaxValue = 100,
                BaseValue = 50
            };
            SelectedCustomControl.RangeValues.Add(rv);
            SelectedCustomControl.Prompt = "已修改，请保存";
            SelectedRangeValue = rv;
        }

        private DelegateCommand _RemoveRangeValueCommand;
        /// <summary>
        /// 删除选中的 RangeValue 行
        /// </summary>
        public DelegateCommand RemoveRangeValueCommand =>
            _RemoveRangeValueCommand ??= new DelegateCommand(ExecuteRemoveRangeValue);

        void ExecuteRemoveRangeValue()
        {
            if (SelectedCustomControl == null || SelectedRangeValue == null) return;
            SelectedCustomControl.RangeValues.Remove(SelectedRangeValue);
            SelectedCustomControl.Prompt = "已修改，请保存";
        }

        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            SelectedCustomControl = parameters.GetValue<CustomControl>("SelectedCustomControl");

            if (SelectedCustomControl != null)
            {
                Title = $"配置: {SelectedCustomControl.Name}";

                if (SelectedCustomControl.Type == ControlType.RangeValueTalbe
                    && SelectedCustomControl.RangeValues.Count == 0)
                {
                    // 初次创建时添加一个默认行
                    SelectedCustomControl.RangeValues.Add(new RangeValue
                    {
                        Name = "Item1",
                        MinValue = 0,
                        MaxValue = 100,
                        BaseValue = 50
                    });
                }
            }
        }
    }
}
