using PLCSharp.Core.Prism;
using PLCSharp.Models;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.Recipe
{
    /// <summary>
    /// Recipe编辑视图模型
    /// </summary>
    public class RecipeEditViewModel : DialogAwareBase
    {
        /// <summary>
        /// Recipe编辑视图模型
        /// </summary>
        public RecipeEditViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)

        {

            Model = container.Resolve<GlobalModel>();
        }

        /// <summary>
        /// 模型
        /// </summary>
        public GlobalModel Model { get; set; }
    }
}
