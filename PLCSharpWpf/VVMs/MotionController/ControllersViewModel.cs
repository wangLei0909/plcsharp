using PLCSharp.Core.Prism;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// Controllers视图模型
    /// </summary>
    public class ControllersViewModel : DialogAwareBase
    {
        /// <summary>
        /// Controllers视图模型
        /// </summary>
        public ControllersViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            ControllerModel = container.Resolve<ControllersModel>();
        }
        /// <summary>
        /// Controller模型
        /// </summary>
        public ControllersModel ControllerModel { get; set; }

        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {

        }
        /// <summary>
        /// 关闭对话框后要执行的
        /// </summary>
        public override void OnDialogClosed()
        {

        }
    }
}
