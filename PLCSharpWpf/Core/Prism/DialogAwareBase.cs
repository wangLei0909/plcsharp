using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;

namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// 对话框通知基类，封装了 Prism IDialogAware 的通用逻辑
    /// </summary>
    public class DialogAwareBase : BindableBase, IDialogAware
    {
        /// <summary>
        /// 构造对话框基类，保存事件聚合器
        /// </summary>
        public DialogAwareBase(IContainerExtension container, IEventAggregator ea, IDialogService dialogService)
        {
            _EventAggregator = ea;

        }


        private string _Title = "";
        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }
        private DelegateCommand<string> _closeDialogCommand;
        /// <summary>
        /// 关闭对话框命令
        /// </summary>
        public DelegateCommand<string> CloseDialogCommand => _closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

        private string _message;

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        /// <summary>
        /// 请求关闭对话框的句柄
        /// </summary>
        public DialogCloseListener RequestClose { get; }

        /// <summary>
        /// 根据参数关闭对话框并返回结果
        /// </summary>
        /// <param name="parameter">按钮参数（ok/yes/no/cancel 等）</param>
        protected virtual void CloseDialog(string parameter)
        {
            var resultstr = parameter?.ToLower();
            var result = resultstr switch
            {
                "ok" or "true" => ButtonResult.OK,
                "yes" => ButtonResult.Yes,
                "no" or "false" => ButtonResult.No,
                "cancel" => ButtonResult.Cancel,
                "ignore" => ButtonResult.Ignore,
                _ => ButtonResult.Cancel,
            };
            RaiseRequestClose(new DialogResult(result));
        }

        /// <summary>
        /// 触发对话框关闭事件，通知调用方结果
        /// </summary>
        /// <param name="dialogResult">对话框结果（包含按钮状态和数据）</param>
        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose.Invoke(dialogResult);
        }

        /// <summary>
        /// 判断对话框是否可以关闭
        /// </summary>
        /// <returns>始终返回 true</returns>
        public virtual bool CanCloseDialog()
        {
            return true;
        }

        /// <summary>
        /// 对话框关闭后的回调，子类可重写进行清理
        /// </summary>
        public virtual void OnDialogClosed()
        {
        }

        /// <summary>
        /// 对话框打开后的回调，解析传入的参数
        /// </summary>
        /// <param name="parameters">传入的参数列表，可从其中获取 message 等参数</param>
        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
            var message = parameters.GetValue<string>("message");
            if (message != null)
            {
                var msg = message.Split(':');

                Title = msg[0];
                if (msg.Length > 1)
                {
                    Message = msg[1];
                }
            }
        }
        /// <summary>
        /// 事件聚合器，用于发送消息和通知
        /// </summary>
        protected readonly IEventAggregator _EventAggregator;
        /// <summary>
        /// 发送错误消息到日志和界面
        /// </summary>
        /// <param name="msg">错误消息内容</param>
        /// <param name="type">消息类型（记录、对话框提示等）</param>
        public void SendErr(string msg, ShowType type,ErrType errType)
        {
            _EventAggregator.GetEvent<MessageEvent>().Publish(new()
            {
                ErrType = errType,
                Type = type,
                Target = "errLog",
                Content = msg
            });
        }

        /// <summary>
        /// 发送故障信息
        /// </summary>
        /// <param name="msg">msg</param>
        public void SendErr(string msg)
        {
            SendErr(msg, ShowType.Record, ErrType.Error);
        }
        /// <summary>
        /// 发送弹出故障消息
        /// </summary>
        /// <param name="msg">msg</param>
        public void SendErrDialog(string msg)
        {
            SendErr(msg, ShowType.ShowDialogAndRecord, ErrType.Error);
        }

        /// <summary>
        /// 发送普通记录信息
        /// </summary>
        /// <param name="msg">msg</param>
        public void SendInfo(string msg)
        {
            SendErr(msg, ShowType.Record, ErrType.Info);
        }

        /// <summary>
        /// 发送弹出消息
        /// </summary>
        /// <param name="msg">msg</param>
        public void SendInfoDialog(string msg)
        {
            SendErr(msg, ShowType.ShowDialogAndRecord, ErrType.Info);
        }
    }
}