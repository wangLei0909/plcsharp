using PLCSharp.Core.Prism;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Windows;

namespace PLCSharp.VVMs.Authority
{
    /// <summary>
    /// 登录模型
    /// </summary>
    [Model]
    public class LoginModel : ModelBase
    {
        /// <summary>
        /// 登录模型
        /// </summary>
        public LoginModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            //已注册用户
            var users = _DatasContext.Users?.ToList();

            if (users is null || users.Count == 0)

            {
                var password = Encrypt("admin");
                var admin = new User("admin", password, Authority.Super);
                UserList.Add(admin);
                password = Encrypt("guest");
                UserList.Add(new("guest", password, Authority.Guset));

                CurrentUser = admin;
                MessageBox.Show("首次运行软件，自动为您创建超级用户 admin 密码 admin 请及时修改密码！ ");
                foreach (var item in UserList)
                {
                    _DatasContext.Users.Add(item);
                }
                _DatasContext.Save();

            }
            else
            {

                foreach (var user in users)
                {
                    UserList.Add(user);
                }
            }

        }

        private ObservableCollection<User> _UserList = [];
        /// <summary>
        /// 用户列表
        /// </summary>
        public ObservableCollection<User> UserList
        {
            get { return _UserList; }
            set { SetProperty(ref _UserList, value); }
        }

        private User _LoginUser = new();
        /// <summary>
        /// 登录的用户
        /// </summary>

        public User LoginUser
        {
            get { return _LoginUser; }
            set
            {
                SetProperty(ref _LoginUser, value);
            }
        }

        private string _Password;
        /// <summary>
        /// 密码
        /// </summary>
        public string Password
        {
            get { return _Password; }
            set { SetProperty(ref _Password, value); CheckPassword(); }
        }
        private string _PasswordAct;
        /// <summary>
        /// 密码Act
        /// </summary>
        public string PasswordAct
        {
            get { return _PasswordAct; }
            set { SetProperty(ref _PasswordAct, value); CheckPassword(); }
        }

        private User _CurrentUser;
        /// <summary>
        /// 当前用户
        /// </summary>
        public User CurrentUser
        {
            get { return _CurrentUser; }
            set
            {
                SetProperty(ref _CurrentUser, value);
                _EventAggregator.GetEvent<MessageEvent>().Publish(new()
                {
                    Target = "CurrentUser"
                });

            }
        }



        private User _RegisterUser = new();
        /// <summary>
        /// 注册用户
        /// </summary>
        public User RegisterUser
        {
            get { return _RegisterUser; }
            set { SetProperty(ref _RegisterUser, value); }
        }




        private User _SelectUser;
        /// <summary>
        /// 选择用户
        /// </summary>
        public User SelectUser
        {
            get { return _SelectUser; }
            set { SetProperty(ref _SelectUser, value); }
        }
        private DelegateCommand<string> _Manage;
        /// <summary>
        /// 管理
        /// </summary>
        public DelegateCommand<string> Manage =>
            _Manage ??= new DelegateCommand<string>(ExecuteManage);

        void ExecuteManage(string cmd)
        {
            switch (cmd)
            {
                case "Register":
                    _dialogService.ShowDialog("Register", new DialogParameters("message=注册:新用户"), r => { });

                    break;

                case "PasswordChange":
                    _dialogService.ShowDialog("PasswordChange", new DialogParameters($"name={SelectUser.Name}"), r => { });
                    break;


                case "Delete":
                    _dialogService.ShowDialog("AlertDialog", new DialogParameters($"message=choose:删除用户{SelectUser.Name}？"), r =>
                    {

                        switch (r.Result)
                        {

                            case ButtonResult.Yes:
                                if (SelectUser != null)
                                {

                                    if (SelectUser.Name == "admin" || SelectUser.Name == "guest")
                                    {
                                        MessageBox.Show($"无法删除{SelectUser.Name}用户");
                                    }
                                    else
                                    {

                                        _DatasContext.Remove(SelectUser);
                                        UserList.Remove(SelectUser);

                                    }

                                }

                                break;
                            default:
                                break;
                        }
                    });

                    break;



            }
        }

        private bool PasswordCheck { get; set; }
        private DelegateCommand _Register;
        /// <summary>
        /// 注册
        /// </summary>
        public DelegateCommand Register =>
            _Register ??= new DelegateCommand(ExecuteRegister);

        void ExecuteRegister()
        {

            if (CheckPassword())
            {

                if (UserList.Where(u => u.Name == RegisterUser.Name).Any())
                {
                    MessageBox.Show($"用户{RegisterUser.Name}已存在！");
                }
                else if (RegisterUser.Authority == 0)
                {
                    MessageBox.Show($"请选择权限！");

                }
                else
                {
                    var newUser = new User(RegisterUser.Name, Encrypt(Password), RegisterUser.Authority);

                    UserList.Add(newUser);
                    _DatasContext.Users.Add(newUser);
                    Password = "";
                    PasswordAct = "";
                    MessageBox.Show($"用户{RegisterUser.Name}注册成功！");

                }
            }


        }

        private string _PasswordMsg = "";
        /// <summary>
        /// 密码Msg
        /// </summary>
        public string PasswordMsg
        {
            get { return _PasswordMsg; }
            set { SetProperty(ref _PasswordMsg, value); }
        }


        private bool CheckPassword()
        {
            PasswordCheck = Password == PasswordAct && Password.Length > 0;

            if (Password.Length == 0)
                PasswordMsg = "密码不能为空";
            else if (!(Password == PasswordAct))
                PasswordMsg = "两次输入不一致";
            else if (PasswordCheck)
                PasswordMsg = "";
            else
                PasswordMsg = "密码不符合要求";

            return PasswordCheck;
        }


        private DelegateCommand _ChangePassword;


        /// <summary>
        /// Change密码
        /// </summary>
        public DelegateCommand ChangePassword =>
            _ChangePassword ??= new DelegateCommand(ExecuteChangePassword);

        void ExecuteChangePassword()
        {
            if (CheckPassword() && SelectUser != null)
            {

                SelectUser.Password = Encrypt(Password);

                var user = _DatasContext.Users.Where(u => u.Name == SelectUser.Name).FirstOrDefault();

                if (user != null)
                {

                    user.Password = SelectUser.Password;
                    Password = "";
                    PasswordAct = "";
                    PasswordMsg = $"用户{SelectUser.Name}密码修改完成！";
                }

            }

        }


        /// <summary>
        /// Encrypt
        /// </summary>
        /// <param name="str">str</param>
        /// <returns>返回字符串</returns>
        public static string Encrypt(string str)
        {
            if (str == null) return "";
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(str, salt, 100000, HashAlgorithmName.SHA256, 32);
            return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verify
        /// </summary>
        /// <param name="password">密码</param>
        /// <param name="stored">stored</param>
        /// <returns>返回布尔值</returns>
        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored) || !stored.Contains('.')) return false;
            var parts = stored.Split('.');
            if (parts.Length != 2) return false;
            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] storedHash = Convert.FromBase64String(parts[1]);
                byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
                return CryptographicOperations.FixedTimeEquals(hash, storedHash);
            }
            catch { return false; }
        }
    }
}