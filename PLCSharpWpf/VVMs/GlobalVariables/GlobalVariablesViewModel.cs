using PLCSharp.Core.Prism;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.GlobalVariables
{
    /// <summary>
    /// 全局Variables视图模型
    /// </summary>
    public class GlobalVariablesViewModel : DialogAwareBase
    {
        /// <summary>
        /// 全局Variables视图模型
        /// </summary>
        public GlobalVariablesViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            Model = container.Resolve<VariablesModel>();
        }
        /// <summary>
        /// 模型
        /// </summary>
        public VariablesModel Model { get; set; }


    }
}