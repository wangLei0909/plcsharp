using PLCSharp.Core.Prism;
using PLCSharp.Models;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Windows.Controls;

namespace PLCSharp.VVMs.ModeState
{
    /// <summary>
    /// State配置
    /// </summary>
    [NavigationPage(ViewName = "StateConfig",
       IconKind = "\ue659",
       DisplayName = "模式状态", UserLevel = Authority.Authority.Guset, Index = 2)]
    public partial class StateConfig : UserControl
    {
        /// <summary>
        /// State配置
        /// </summary>
        public StateConfig()
        {
            InitializeComponent();
        }
    }


    /// <summary>
    /// State配置视图模型
    /// </summary>
    public class StateConfigViewModel : DialogAwareBase
    {
        /// <summary>
        /// State配置视图模型
        /// </summary>
        public StateConfigViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)

        {
            GlobalModel = container.Resolve<GlobalModel>();
            Title = "状态设置";
        }

        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }
    }


}
