using PLCSharp.Core.Prism;
using PLCSharp.Models;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.Windows;

namespace PLCSharp.VVMs.GlobalVariables
{
    /// <summary>
    /// Variables模型
    /// </summary>
    [Model]
    public partial class VariablesModel : ModelBase
    {

        /// <summary>
        /// Variables模型
        /// </summary>
        public VariablesModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {


        }
        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="globalModel">全局模型</param>
        public void Init(GlobalModel globalModel)
        {
            GlobalModel = globalModel;

            foreach (var item in Variables)
            {
                item._DatasContext = _DatasContext;
            }
            foreach (var item in SystemVariables)
            {
                item._DatasContext = _DatasContext;
            }
        }



        /// <summary>
        /// 局部变量集合
        /// </summary>
        public ObservableCollection<Variable> Variables { get; set; } = [];
        public ObservableCollection<SystemVariable> SystemVariables { get; set; } = [];

        private Variable _SelectedVariable;
        /// <summary>
        /// 选中的全局变量
        /// </summary>
        public Variable SelectedVariable
        {
            get { return _SelectedVariable; }
            set { SetProperty(ref _SelectedVariable, value); }
        }

        private DelegateCommand<object> _VariablesManage;
        public DelegateCommand<object> VariablesManage =>
            _VariablesManage ??= new DelegateCommand<object>(ExecuteVariablesManage);

        void ExecuteVariablesManage(object param)
        {
            var cmd = param as string;
            switch (cmd)
            {
                case "New":
                    Variables.Add(new() { RecipeID = GlobalModel.CurrentRecipe.ID, _DatasContext = _DatasContext });
                    break;
                case "Delete":

                    if (SelectedVariable != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除  [{SelectedVariable.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        _DatasContext.Variables.Remove(SelectedVariable);
                        var name = SelectedVariable.Name;
                        Variables.Remove(SelectedVariable);
                        SendInfoDialog($"已删除变量：{name}");
                    }
                    break;
                case "Save":

                    var names = new List<string>();

                    foreach (var item in Variables)
                    {
                        if (string.IsNullOrEmpty(item.Name))
                        {
                            SendInfoDialog("保存失败，无变量名！");
                            return;
                        }

                        if (names.Contains(item.Name))
                        {

                            SendInfoDialog("保存失败，重名的变量！");
                            return;

                        }
                        else
                        {
                            names.Add(item.Name);
                        }

                    }


                    foreach (var item in Variables)
                    {
                        if (!_DatasContext.Variables.Contains(item))
                        {
                            _DatasContext.Variables.Add(item);
                        }

                    }
                    _DatasContext.Save();

                    SendInfoDialog("保存成功！");
                    break;
            }

        }






    }
}