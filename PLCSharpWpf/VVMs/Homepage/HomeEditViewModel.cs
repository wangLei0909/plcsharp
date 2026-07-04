using PLCSharp.Core.Prism;
using PLCSharp.Models;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;

namespace PLCSharp.VVMs.Homepage
{
    /// <summary>
    /// 回零编辑视图模型
    /// </summary>
    public class HomeEditViewModel : DialogAwareBase
    {
        /// <summary>
        /// 回零编辑视图模型
        /// </summary>
        public HomeEditViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)

        {
            Model = container.Resolve<GlobalModel>();
        }
        /// <summary>
        /// 模型
        /// </summary>
        public GlobalModel Model { get; }

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
                case "New":
                    Model.CustomControls.Add(new CustomControl() { RecipeID = Model.CurrentRecipe.ID });

                    break;

                case "Save":
                    if (Model.SelectedCustomControl == null) return;
                    var names = new List<string>();

                    foreach (var item in Model.CustomControls)
                    {
                        if (string.IsNullOrEmpty(item.Name))
                        {
                            SendInfoDialog($"保存失败，名称{item.Name}不合适！");
                            return;
                        }

                        if (names.Contains(item.Name))
                        {
                            SendInfoDialog($"保存失败，重复的名称{item.Name}！");
                            return;
                        }
                        else
                        {
                            names.Add(item.Name);
                        }
                    }


                    foreach (var item in Model.CustomControls)
                    {
                        if (!Model._DatasContext.CustomControls.Any(h => h.ID == item.ID))
                        {
                            Model._DatasContext.CustomControls.Add(item);

                        }
                        else
                        {
                            var control = Model._DatasContext.CustomControls.Where(c => c.ID == item.ID).FirstOrDefault();
                            control.RecipeID = item.RecipeID;
                            control.Name = item.Name;
                            control.Type = item.Type;
                            control.Row = item.Row;
                            control.RowSpan = item.RowSpan;
                            control.Column = item.Column;
                            control.ColumnSpan = item.ColumnSpan;
                            control.Params = item.Params;
                            control.Comment = item.Comment;
                            control.SerializedRangeValues = item.SerializedRangeValues;
                        }


                    }
                    Model.SelectedCustomControl.Prompt = "";

                    break;

                case "Config":
                    var dialogParams = new DialogParameters
                        {
                            { "SelectedCustomControl", Model.SelectedCustomControl }
                        };
                    Model._dialogService.Show("CustomControlConfig", dialogParams, r =>
                    {


                    });
                    break;

                case "Remove":
                    if (Model.SelectedCustomControl != null)
                    {
                        var control = Model._DatasContext.CustomControls.Where(c => c.ID == Model.SelectedCustomControl.ID).FirstOrDefault();
                        if (control != null)
                        {

                            Model._DatasContext.CustomControls.Remove(control);
                        }
                        var name = Model.SelectedCustomControl.Name;
                        Model.CustomControls.Remove(Model.SelectedCustomControl);
                        SendInfoDialog($"已删除：{name}");
                        Model._DatasContext.Save();
                    }
                    break;

            }
        }
    }
}