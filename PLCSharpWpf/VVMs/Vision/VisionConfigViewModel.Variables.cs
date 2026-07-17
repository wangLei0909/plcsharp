using PLCSharp.VVMs.GlobalVariables;
using Prism.Commands;
using Prism.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;

namespace PLCSharp.VVMs.Vision;

/// <summary>
/// 视觉流程配置界面 —— 局部变量表和系统变量管理
/// </summary>
public partial class VisionConfigViewModel
{
    #region 全局变量

    /// <summary>
    /// 系统全局变量集合
    /// </summary>
    public ObservableCollection<SystemVariable> SystemVariables { get; set; }


    private SystemVariable _SelectedSystemVariable;
    /// <summary>
    /// 选中的全局变量
    /// </summary>
    public SystemVariable SelectedSystemVariable
    {
        get { return _SelectedSystemVariable; }
        set
        {
            SetProperty(ref _SelectedSystemVariable, value);

        }
    }

    private DelegateCommand<object> _SystemVariablesManage;
    public DelegateCommand<object> SystemVariablesManage =>
        _SystemVariablesManage ??= new DelegateCommand<object>(ExecuteVariablesManage);

    void ExecuteVariablesManage(object param)
    {
        var cmd = param as string;
        switch (cmd)
        {
            case "New":
                SystemVariables.Add(new() { RecipeID = GlobalModel.CurrentRecipe.ID, _DatasContext = GlobalModel._DatasContext });
                break;
            case "Delete":

                if (SelectedSystemVariable != null)
                {
                    if (System.Windows.MessageBox.Show($"确认删除  [{SelectedSystemVariable.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        break;
                    GlobalModel._DatasContext.SystemVariables.Remove(SelectedSystemVariable);
                    SystemVariables.Remove(SelectedSystemVariable);
                    var name = SelectedSystemVariable.Name;
                    SendInfoDialog($"已删除变量：{name}");
                }
                break;
            case "Save":

                var names = new List<string>();

                foreach (var item in SystemVariables)
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

                foreach (var item in SystemVariables)
                {
                    if (!GlobalModel._DatasContext.SystemVariables.Contains(item))
                    {
                        GlobalModel._DatasContext.SystemVariables.Add(item);
                    }

                }
                GlobalModel._DatasContext.Save();

                SendInfoDialog("保存成功！");
                break;
        }
    }
    #endregion
    #region 局部变量
    private LocalVariableItem _SelectedLocalVariable;
    /// <summary>
    /// 局部变量表中选中的行
    /// </summary>
    public LocalVariableItem SelectedLocalVariable
    {
        get { return _SelectedLocalVariable; }
        set { SetProperty(ref _SelectedLocalVariable, value); }
    }

    private DelegateCommand<string> _LocalVariableManage;
    /// <summary>
    /// 局部变量表右键菜单命令（AddPoint / AddLine / AddCircle / AddRect / Remove）
    /// </summary>
    public DelegateCommand<string> LocalVariableManage =>
        _LocalVariableManage ??= new DelegateCommand<string>(ExecuteLocalVariableManage);

    void ExecuteLocalVariableManage(string cmd)
    {
        if (SelectedVisionFunction == null) return;
        var vars = SelectedVisionFunction.Params.Variables;

        switch (cmd)
        {
            case "Edit":
                if (_SelectedLocalVariable != null)
                {
                    _dialogService.ShowDialog("VariableEditor", new DialogParameters { { "item", _SelectedLocalVariable } }, _ =>
                    {

                    });
                }
                break;
            case "AddPoint":
                vars.Add(new LocalVariableItem(NextVarName(vars, "point"), "Pos", new Pos()));
                break;
            case "AddLine":
                vars.Add(new LocalVariableItem(NextVarName(vars, "line"), "Line", new Line()));
                break;
            case "AddCircle":
                vars.Add(new LocalVariableItem(NextVarName(vars, "circle"), "Circle", new Circle()));
                break;
            case "AddRect":
                vars.Add(new LocalVariableItem(NextVarName(vars, "rect"), "Rect", new Rect()));
                break;
            case "Remove":
                if (_SelectedLocalVariable != null && vars.Contains(_SelectedLocalVariable))
                    vars.Remove(_SelectedLocalVariable);
                break;
        }
    }

    private static string NextVarName(ObservableCollection<LocalVariableItem> vars, string baseName)
    {
        var existing = vars.Select(v => v.Name).ToHashSet();
        int i = 1;
        while (existing.Contains($"{baseName}{i}"))
            i++;
        return $"{baseName}{i}";
    }


    #endregion
}
