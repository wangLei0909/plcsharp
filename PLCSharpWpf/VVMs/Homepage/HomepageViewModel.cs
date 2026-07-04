using PLCSharp.Core.Prism;
using PLCSharp.Models;
using Prism.Dialogs;
using Prism.Ioc;

namespace PLCSharp.VVMs.Homepage
{
    /// <summary>
    /// Homepage视图模型
    /// </summary>
    public class HomepageViewModel : ViewModelBase
    {

        /// <summary>
        /// Homepage视图模型
        /// </summary>
        public HomepageViewModel(IContainerExtension container, IDialogService dialogService)
        {

            GlobalModel = container.Resolve<GlobalModel>();

        }


        /// <summary>
        /// 模型
        /// </summary>
        public GlobalModel GlobalModel { get; }


    }
}