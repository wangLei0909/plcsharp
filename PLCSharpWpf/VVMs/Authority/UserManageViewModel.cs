using PLCSharp.Core.Prism;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.Authority
{
    /// <summary>
    /// 用户管理视图模型
    /// </summary>
    public class UserManageViewModel : DialogAwareBase
    {
        /// <summary>
        /// 模型
        /// </summary>
        public LoginModel Model { get; set; }

        /// <summary>
        /// 用户管理视图模型
        /// </summary>
        public UserManageViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            Model = container.Resolve<LoginModel>();
            Title = "用户管理";
        }


    }
}