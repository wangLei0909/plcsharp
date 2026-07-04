using PLCSharp.Core.Prism;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.Authority
{
    /// <summary>
    /// 注册视图模型
    /// </summary>
    public class RegisterViewModel : DialogAwareBase
    {
        /// <summary>
        /// 注册视图模型
        /// </summary>
        public RegisterViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            Model = container.Resolve<LoginModel>();
            Model.RegisterUser.Authority = Authority.Operator;
        }

        /// <summary>
        /// 模型
        /// </summary>
        public LoginModel Model { get; set; }


    }
}
