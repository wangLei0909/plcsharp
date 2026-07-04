using PLCSharp.Core.Prism;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.Authority
{
    /// <summary>
    /// 密码Change视图模型
    /// </summary>
    public class PasswordChangeViewModel : DialogAwareBase
    {
        private string _Name;

        /// <summary>
        /// 密码Change视图模型
        /// </summary>
        public PasswordChangeViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            Model = container.Resolve<LoginModel>();
        }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }



        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            Name = parameters.GetValue<string>("name");
            Title = $" 正在修改用户: {Name} 的密码";
        }

        /// <summary>
        /// 模型
        /// </summary>
        public LoginModel Model { get; set; }




    }
}