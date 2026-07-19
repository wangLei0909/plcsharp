using PLCSharp.Core.Prism;
using PLCSharp.Core.UserControls;
using PLCSharp.VVMs.Homepage;
using Prism.Commands;
using System.Collections.ObjectModel;

namespace PLCSharp.Models
{
    /// <summary>
    /// 全局模型 - 主页画布管理
    /// </summary>
    public partial class GlobalModel
    {
        #region 主页面
        private ObservableCollection<CustomControl> _CustomControls = [];
        /// <summary>
        /// 配置项
        /// </summary>
        public ObservableCollection<CustomControl> CustomControls
        {
            get { return _CustomControls; }
            set { SetProperty(ref _CustomControls, value); }
        }

        /// <summary>
        /// 获取Custom控件
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>返回结果</returns>
        public CustomControl GetCustomControl(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            else
            {
                return CustomControls.Where(c => c.Name == name).FirstOrDefault();
            }
        }

        public ImageEdit GetImageControl(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            else
            {
                var control = CustomControls.Where(c => c.Name == name).FirstOrDefault();

                if (control != null)
                {

                    if (control.UIElement is ImageEdit imageEdit)
                    {

                        return imageEdit;
                    }
                    else
                    {

                        return null;
                    }

                }
                else
                {
                    return null;
                }
            }
        }
        private CustomControl _SelectedCustControl;
        /// <summary>
        /// 配置项
        /// </summary>
        public CustomControl SelectedCustomControl
        {
            get { return _SelectedCustControl; }
            set { SetProperty(ref _SelectedCustControl, value); }
        }

        private CanvasConfig _CurrentCanvasConfig;
        /// <summary>
        /// 配置项
        /// </summary>
        public CanvasConfig CurrentCanvasConfig
        {
            get { return _CurrentCanvasConfig; }
            set { SetProperty(ref _CurrentCanvasConfig, value); }
        }

        private DelegateCommand _Apply;
        /// <summary>
        /// Apply
        /// </summary>
        public DelegateCommand Apply =>
            _Apply ??= new DelegateCommand(ExecuteApply);

        void ExecuteApply()
        {

            var config = _DatasContext.CanvasConfigs.Where(c => c.RecipeID == CurrentRecipe.ID).FirstOrDefault();
            if (config != null)
            {
                config.Rows = CurrentCanvasConfig.Rows;
                config.Columns = CurrentCanvasConfig.Columns;
            }
            else
            {
                _DatasContext.CanvasConfigs.Add(CurrentCanvasConfig);

            }
            //发布重新加载主页面通知
            _EventAggregator.GetEvent<MessageEvent>().Publish(new()
            {
                Target = "UIReLoad"
            });
            _DatasContext.Save();
        }
        #endregion
    }
}
