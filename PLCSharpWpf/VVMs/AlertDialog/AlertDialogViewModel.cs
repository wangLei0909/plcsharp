using PLCSharp.Core.Prism;
using PLCSharp.Core.Resources.Iconfont;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace PLCSharp.VVM.AlertDialog
{
    /// <summary>
    /// Alert对话框视图模型
    /// </summary>
    public class AlertDialogViewModel : DialogAwareBase
    {
        private DelegateCommand<string> _closeDialogCommand;

        /// <summary>
        /// 关闭对话框命令
        /// </summary>
        public new DelegateCommand<string> CloseDialogCommand =>
                _closeDialogCommand ??= new DelegateCommand<string>(ExecuteCloseDialogCommand);

        private void ExecuteCloseDialogCommand(string parameter)
        {
            ButtonResult result = ButtonResult.None;
            if (parameter?.ToLower() == "true")
                result = ButtonResult.Yes;
            else if (parameter?.ToLower() == "false")
                result = ButtonResult.No;
            RaiseRequestClose(new DialogResult(result));
        }


        private string _Ico = "\ueb65";

        /// <summary>
        /// Alert对话框视图模型
        /// </summary>
        public AlertDialogViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
        }

        /// <summary>
        /// 图标
        /// </summary>
        public string Ico
        {
            get { return _Ico; }
            set { SetProperty(ref _Ico, value); }
        }


        /// <summary>
        /// 打开对话框后要执行的
        /// </summary>
        /// <param name="parameters">parameters</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            Source.Add(new Button()
            {
                Content = "确认",
                Background = Brushes.Green,
                Command = CloseDialogCommand,
                CommandParameter = "true",
                Width = 50,
                Height = 30
            });

            var message = parameters.GetValue<string>("message");
            if (message != null)
            {
                var msg = message.Split(':');
                Title = msg[0];
                if (msg.Length > 1)
                {
                    Message = msg[1];
                }
                else
                {

                    Message = msg[0];

                }
                Ico = Title switch
                {
                    "message" => Icon.服务,
                    "choose" => Icon.菜单,
                    _ => Icon.警告,
                };

                if (Title == "choose")
                    Source.Add(new Button()
                    {
                        Content = "取消",
                        Background = Brushes.Red,
                        Command = CloseDialogCommand,
                        CommandParameter = "false",
                        Width = 50,
                        Height = 30
                    });
            }
        }

        /// <summary>
        /// 按钮列表
        /// </summary>
        public ObservableCollection<Button> Source { get; set; } = [];

    }
}
