using PLCSharp.Core.Prism;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
namespace PLCSharp.VVMs.Workflows
{
    /// <summary>
    /// Workflows视图模型
    /// </summary>
    public class WorkflowsViewModel : DialogAwareBase
    {

        /// <summary>
        /// Workflows视图模型
        /// </summary>
        public WorkflowsViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)

        {

            WorkflowsModel = container.Resolve<WorkflowsModel>();
        }

        /// <summary>
        /// Workflows模型
        /// </summary>
        public WorkflowsModel WorkflowsModel { get; set; }


    }
}