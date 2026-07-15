using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLCSharp.VVMs.Recipe
{
    /// <summary>
    /// Recipe
    /// </summary>
    public class Recipe : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
        private string _Name;
        /// <summary>
        /// 配置项
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name != value)
                {
                    Prompt = "已修改，请保存";
                }
                SetProperty(ref _Name, value);
            }
        }
        private string _Comment;
        /// <summary>
        /// 备注
        /// </summary>
        public string Comment

        {
            get { return _Comment; }
            set
            {
                if (_Comment != value)
                {
                    Prompt = "已修改，请保存";
                }
                SetProperty(ref _Comment, value);
            }
        }

        private bool _Current;
        /// <summary>
        /// 配置项
        /// </summary>
        public bool Current
        {
            get { return _Current; }
            set { SetProperty(ref _Current, value); }
        }


        private string _Prompt;

        /// <summary>
        /// 提示
        /// </summary>
        [NotMapped]
        public string Prompt
        {
            get { return _Prompt; }
            set { SetProperty(ref _Prompt, value); }
        }
    }
}
