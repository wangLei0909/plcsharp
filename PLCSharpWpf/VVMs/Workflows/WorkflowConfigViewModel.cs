using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.Models;
using PLCSharp.VVMs.Homepage;
using PLCSharp.VVMs.Workflows.Script;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PLCSharp.VVMs.Workflows
{
    /// <summary>
    /// 工作流配置视图模型
    /// </summary>
    public class WorkflowConfigViewModel : DialogAwareBase
    {
        /// <summary>
        /// 工作流配置视图模型
        /// </summary>
        public WorkflowConfigViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)

        {
            GlobalModel = container.Resolve<GlobalModel>();
        }

        private AsyncDelegateCommand _Save;
        /// <summary>
        /// 保存
        /// </summary>
        public AsyncDelegateCommand Save =>
            _Save ??= new AsyncDelegateCommand(ExecuteSaveAsync);

        private async Task ExecuteSaveAsync()
        {
            await CustomTextEditor.FormatCodeAsync();
            GlobalModel.WorkflowsModel.Manage.Execute("Save");

        }

        private AsyncDelegateCommand _Compile;
        /// <summary>
        /// 编译
        /// </summary>
        public AsyncDelegateCommand Compile =>
            _Compile ??= new AsyncDelegateCommand(ExecuteCompileAsync);

        private async Task ExecuteCompileAsync()
        {

            await CustomTextEditor.FormatCodeAsync();
            GlobalModel.WorkflowsModel.Manage.Execute("Save");
            SelectedFlowTask.Compile.Execute();
        }

        private Workflow _SelectedFlowTask;
        /// <summary>
        /// SelectedFlowTask
        /// </summary>
        public Workflow SelectedFlowTask
        {
            get { return _SelectedFlowTask; }
            set { SetProperty(ref _SelectedFlowTask, value); }
        }

        /// <summary>
        /// 快捷代码片段列表
        /// </summary>
        public ObservableCollection<SnippetItem> SnippetItems { get; set; } = [];

        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            SelectedFlowTask = parameters.GetValue<Workflow>("SelectedTask");
            LoadSnippets();
        }

        /// <summary>
        /// 从 Script 目录加载 .spt 文件
        /// </summary>
        private void LoadSnippets()
        {
            SnippetItems.Clear();

            // 从程序集所在目录逐级向上查找 Script 目录
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            string scriptDir;
            while (true)
            {
                scriptDir = Path.Combine(dir, @"VVMs\Workflows\Script");
                if (Directory.Exists(scriptDir)) break;

                var parent = Directory.GetParent(dir);
                if (parent == null) return;
                dir = parent.FullName;
            }

            foreach (var file in Directory.GetFiles(scriptDir, "*.spt").OrderBy(f => f))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var content = File.ReadAllText(file);
                SnippetItems.Add(new SnippetItem { Name = name, Content = content });
            }
        }

        private DelegateCommand _Run;
        /// <summary>
        /// 运行
        /// </summary>
        public DelegateCommand Run =>
            _Run ??= new DelegateCommand(ExecuteRun);

        void ExecuteRun()
        {
            if (SelectedFlowTask.AutomaticExecution == false)
            {
                SelectedFlowTask.Run(GlobalModel);
            }
            else
            {
                System.Windows.MessageBox.Show("自动运行中，不能手动运行!"); ;
            }
        }
        readonly GlobalModel GlobalModel;


        private DelegateCommand<string> _FastEntry;
        /// <summary>
        /// FastEntry
        /// </summary>
        public DelegateCommand<string> FastEntry =>
            _FastEntry ??= new DelegateCommand<string>(ExecuteFastEntry);

        void ExecuteFastEntry(string content)
        {
            if (!string.IsNullOrEmpty(content))
                CustomTextEditor.Insert(content);
        }

        /// <summary>
        /// CustomTextEditor
        /// </summary>
        public CustomTextEditor CustomTextEditor { get; set; }
    }
}
