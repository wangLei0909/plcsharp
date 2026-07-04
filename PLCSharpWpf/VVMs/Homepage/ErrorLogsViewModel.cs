using PLCSharp.Core.Prism;
using PLCSharp.Models;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.Homepage
{
    /// <summary>
    /// 错误Logs视图模型
    /// </summary>
    public class ErrorLogsViewModel : DialogAwareBase
    {
        /// <summary>
        /// 错误Logs视图模型
        /// </summary>
        public ErrorLogsViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            Model = container.Resolve<GlobalModel>();
        }

        /// <summary>
        /// 模型
        /// </summary>
        public GlobalModel Model { get; set; }
    }
}
