using PLCSharp.Core.Prism;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Workflows
{
    /// <summary>
    /// 工作流配置
    /// </summary>
    [Dialog]
    /// <summary>
    /// WorkflowConfig.xaml 的交互逻辑
    /// </summary>
    public partial class WorkflowConfig : UserControl
    {
        /// <summary>
        /// 工作流配置
        /// </summary>
        public WorkflowConfig()
        {
            InitializeComponent();
            var dc = DataContext as WorkflowConfigViewModel;
            dc.CustomTextEditor = editor;
            editor.CaretPositionChanged += OnEditorCaretPositionChanged;
            editor.FontSizeChanged += OnEditorFontSizeChanged;
        }

        private void OnEditorCaretPositionChanged(object sender, (int Line, int Column) pos)
        {
            tbStatus.Text = $"行: {pos.Line}  列: {pos.Column}";
        }

        private void OnEditorFontSizeChanged(object sender, double size)
        {
            tbFontSize.Text = $"字号: {size:F0}";
        }
    }
}
