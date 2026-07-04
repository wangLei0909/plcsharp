using PLCSharp.Core.Prism;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.Connects
{
    /// <summary>
    /// Connects视图模型
    /// </summary>
    public class ConnectsViewModel : DialogAwareBase
    {

        /// <summary>
        /// Connects视图模型
        /// </summary>
        public ConnectsViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)

        {
            ConnectsModel = container.Resolve<ConnectsModel>();
        }
        /// <summary>
        /// Connects模型
        /// </summary>
        public ConnectsModel ConnectsModel { get; set; }


    }
}
