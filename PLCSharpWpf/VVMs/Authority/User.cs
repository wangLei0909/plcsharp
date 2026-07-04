using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;

namespace PLCSharp.VVMs.Authority
{


    /// <summary>
    /// 用户
    /// </summary>
    public class User : BindableBase
    {
        /// <summary>
        /// 用户
        /// </summary>
        public User() { }
        /// <summary>
        /// 用户
        /// </summary>
        public User(string name, string password, Authority authority)
        {

            Name = name;
            Password = password;
            Authority = authority;
        }


        private string _Name;
        /// <summary>
        /// 名称
        /// </summary>
        [Key]
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        private Authority _Authority;
        /// <summary>
        /// 权限
        /// </summary>
        public Authority Authority
        {
            get { return _Authority; }
            set { SetProperty(ref _Authority, value); }
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns>返回字符串</returns>
        public override string ToString()
        {
            return $"{Name}";
        }
    }
}