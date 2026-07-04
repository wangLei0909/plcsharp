using PLCSharp.Core.Prism;
using PLCSharp.VVMs.Authority;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation.Regions;
using System.Collections.ObjectModel;

namespace PLCSharp.Models
{
    /// <summary>
    /// Navigate模型
    /// </summary>
    [Model]
    public class NavigateModel : ModelBase
    {
        /// <summary>
        /// Navigate模型
        /// </summary>
        public NavigateModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService, IRegionManager regionManager) : base(container, ea, dialogService)
        {
            _regionManager = regionManager;
            Login = container.Resolve<LoginModel>();
            //注册发送给errLog的消息
            _EventAggregator.GetEvent<MessageEvent>().Subscribe(
                UserChageReceived,
                ThreadOption.UIThread,
                false,
                (filter) => filter.Target.Contains("CurrentUser"));
        }

        private void UserChageReceived(Message message)
        {

            NavigateListShow.Clear();
            var nl = NavigateList.OrderBy(n => n.Index);


            foreach (var item in nl)
            {
                item.Display = Login.CurrentUser.Authority >= item.UserLevel;

                if (item.Display) { NavigateListShow.Add(item); }
            }

            DialogListShow.Clear();

            var dl = DialogList.OrderBy(n => n.Index);


            foreach (var item in dl)
            {
                item.Display = Login.CurrentUser.Authority >= item.UserLevel;

                if (item.Display) { DialogListShow.Add(item); }
            }

        }

        private ObservableCollection<NavigateItem> _NavigateListShow = [];
        /// <summary>
        /// Navigate列表显示
        /// </summary>
        public ObservableCollection<NavigateItem> NavigateListShow
        {
            get { return _NavigateListShow; }
            set { SetProperty(ref _NavigateListShow, value); }
        }


        private ObservableCollection<NavigateItem> _NavigateList = [];
        /// <summary>
        /// 导航页列表
        /// </summary>

        public ObservableCollection<NavigateItem> NavigateList
        {
            get { return _NavigateList; }
            set { SetProperty(ref _NavigateList, value); }
        }

        private ObservableCollection<NavigateItem> _DialogList = [];
        /// <summary>
        /// 配置项
        /// </summary>
        public ObservableCollection<NavigateItem> DialogList
        {
            get { return _DialogList; }
            set { SetProperty(ref _DialogList, value); }
        }

        private ObservableCollection<NavigateItem> _DialogListShow = [];
        /// <summary>
        /// 配置项
        /// </summary>
        public ObservableCollection<NavigateItem> DialogListShow
        {
            get { return _DialogListShow; }
            set { SetProperty(ref _DialogListShow, value); }
        }


        private NavigateItem _DialogTarget;
        /// <summary>
        /// 配置项
        /// </summary>
        public NavigateItem DialogTarget
        {
            get { return _DialogTarget; }
            set
            {
                SetProperty(ref _DialogTarget, value);

                if (value is not null)
                {

                    //   _dialogService.Show(value.ViewName);

                }
            }
        }

        private string _DefaultView;

        /// <summary>
        /// 初始页
        /// </summary>
        public string DefaultView
        {
            get { return _DefaultView; }
            set { SetProperty(ref _DefaultView, value); }
        }

        private NavigateItem _NavigateTarget;

        /// <summary>
        /// 目标
        /// </summary>
        public NavigateItem NavigateTarget
        {
            get { return _NavigateTarget; }
            set
            {
                SetProperty(ref _NavigateTarget, value);
                if (value is not null)
                {
                    NavigateTo(value.ViewName);
                }
            }
        }

        //导航
        private readonly IRegionManager _regionManager;

        /// <summary>
        /// NavigateTo
        /// </summary>
        /// <param name="navigatePath">navigate路径</param>
        public void NavigateTo(string navigatePath)
        {
            if (string.IsNullOrEmpty(navigatePath))
                return;

            var navigate = NavigateList.Where(n => n.ViewName == navigatePath).FirstOrDefault();

            if (navigate == null) return;
            if (_NavigateTarget != navigate)
            {
                _NavigateTarget = navigate;
            }

            //将导航目标发到区域
            _regionManager.RequestNavigate("ContentRegionCore", navigatePath);
        }
        /// <summary>
        /// NavigateToAsync
        /// </summary>
        /// <param name="navigatePath">navigate路径</param>
        /// <returns>返回结果</returns>
        public async Task NavigateToAsync(string navigatePath)
        {
            await Task.Delay(1);
            if (string.IsNullOrEmpty(navigatePath))
                return;

            var navigate = NavigateList.Where(n => n.ViewName == navigatePath).FirstOrDefault();

            if (navigate == null) return;
            if (_NavigateTarget != navigate)
            {
                _NavigateTarget = navigate;
            }

            //将导航目标发到区域
            _regionManager.RequestNavigate("ContentRegionCore", navigatePath);
        }

        /// <summary>
        /// 登录
        /// </summary>
        public LoginModel Login { get; set; }



    }
}