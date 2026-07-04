using PLCSharp.Core.Prism;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Windows;

namespace PLCSharp.VVMs.Authority
{
    /// <summary>
    /// 登录视图模型
    /// </summary>
    public class LoginViewModel : DialogAwareBase
    {
        /// <summary>
        /// 模型
        /// </summary>
        public LoginModel Model { get; set; }
        /// <summary>
        /// 登录视图模型
        /// </summary>
        public LoginViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            Model = container.Resolve<LoginModel>();
            Title = "登录";
        }
        private DelegateCommand<string> _closeDialogCommand;

        /// <summary>
        /// 关闭对话框命令
        /// </summary>
        public new DelegateCommand<string> CloseDialogCommand =>
                _closeDialogCommand ??= new DelegateCommand<string>(ExecuteCloseDialogCommand);

        private void ExecuteCloseDialogCommand(string parameter)
        {

            switch (parameter)
            {
                case "Login":
                    if (Model.SelectUser == null) return;
                    var user = Model.UserList.Where(u => LoginModel.Verify(Model.Password, u.Password)
                      && u.Name == Model.SelectUser.Name).FirstOrDefault();

                    if (user == null)
                    {
                        PasswordMsg = "用户名或密码错误";
                        Model.CurrentUser = Model.UserList.Where(u => u.Name == "guest").FirstOrDefault();
                    }
                    else
                    {
                        Model.CurrentUser = user;


                    }
                    break;
                case "Logout":
                    Model.CurrentUser = Model.UserList.Where(u => u.Name == "guest").FirstOrDefault();
                    PasswordMsg = "已注销，当前用户：游客guest";

                    break;
                default:
                    break;
            }


            ButtonResult result = ButtonResult.None;

            if (parameter?.ToLower() == "manage")
            {
                if (Model.CurrentUser != null && Model.CurrentUser.Authority >= Authority.Administrator)
                {
                    result = ButtonResult.Retry;
                }
                else
                {
                    MessageBox.Show("管理用户需要管理员权限", "无权限");
                    return;
                }
               
            }
            RaiseRequestClose(new DialogResult(result));
        }

        private string _PasswordMsg;
        /// <summary>
        /// 密码Msg
        /// </summary>
        public string PasswordMsg
        {
            get { return _PasswordMsg; }
            set { SetProperty(ref _PasswordMsg, value); }
        }



    }
}