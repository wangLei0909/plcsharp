using PLCSharp.VVMs.Authority;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// Navigate项
    /// </summary>
    public class NavigateItem : BindableBase, IComparable<NavigateItem>
    {
        /// <summary>
        /// Navigate项
        /// </summary>
        public NavigateItem(IDialogService dialogService)
        {
            _dialogService = dialogService;

        }
        private string _ViewName;

        /// <summary>
        /// 视图名称
        /// </summary>
        public string ViewName
        {
            get { return _ViewName; }
            set { SetProperty(ref _ViewName, value); }
        }

        private string _IconKind;

        /// <summary>
        /// IconKind
        /// </summary>
        public string IconKind
        {
            get { return _IconKind; }
            set { SetProperty(ref _IconKind, value); }
        }

        private string _DisplayName;

        /// <summary>
        /// Display名称
        /// </summary>
        public string DisplayName
        {
            get { return _DisplayName; }
            set { SetProperty(ref _DisplayName, value); }
        }

        private Authority _UserLevel;

        /// <summary>
        /// 用户Level
        /// </summary>
        public Authority UserLevel
        {
            get { return _UserLevel; }
            set { SetProperty(ref _UserLevel, value); }
        }

        private bool _Display;

        /// <summary>
        /// Display
        /// </summary>
        public bool Display
        {
            get { return _Display; }
            set { SetProperty(ref _Display, value); }
        }

        /// <summary>
        /// 当前索引
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// CompareTo
        /// </summary>
        /// <param name="other">other</param>
        /// <returns>返回整数值</returns>
        public int CompareTo(NavigateItem other)
        {
            if (other == null) return 1;
            return Index.CompareTo(other.Index);
        }


        private DelegateCommand _Open;
        /// <summary>
        /// 打开
        /// </summary>
        public DelegateCommand Open =>
            _Open ??= new DelegateCommand(ExecuteOpen);

        void ExecuteOpen()
        {
            if (ShowList.Contains(ViewName) == false)
            {
                ShowList.Add(ViewName);
                try
                {


                    _dialogService.Show(ViewName, r =>
                    {
                        if (ShowList.Contains(ViewName))
                        {

                            ShowList.Remove(ViewName);
                        }

                    });
                }
                catch (Exception)
                {

                    ShowList.Remove(ViewName);
                }
            }
        }

        private List<string> ShowList = [];
        IDialogService _dialogService { get; set; }
    }
}