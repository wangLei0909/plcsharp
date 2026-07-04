using Prism.Navigation.Regions;

namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// Region视图模型Base
    /// </summary>
    public class RegionViewModelBase(IRegionManager regionManager) : ViewModelBase, INavigationAware, IConfirmNavigationRequest
    {
        /// <summary>
        /// RegionManager
        /// </summary>
        protected IRegionManager RegionManager { get; private set; } = regionManager;

        /// <summary>
        /// ConfirmNavigationRequest
        /// </summary>
        /// <param name="navigationContext">navigationContext</param>
        /// <param name="continuationCallback">continuationCallback</param>
        public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            continuationCallback(true);
        }

        /// <summary>
        /// IsNavigationTarget
        /// </summary>
        /// <param name="navigationContext">navigationContext</param>
        /// <returns>返回布尔值</returns>
        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <summary>
        /// OnNavigatedFrom
        /// </summary>
        /// <param name="navigationContext">navigationContext</param>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        /// <summary>
        /// OnNavigatedTo
        /// </summary>
        /// <param name="navigationContext">navigationContext</param>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
        }
    }
}