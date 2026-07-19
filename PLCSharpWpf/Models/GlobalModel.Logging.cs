using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using Prism.Dialogs;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace PLCSharp.Models
{
    /// <summary>
    /// 全局模型 - 日志管理
    /// </summary>
    public partial class GlobalModel
    {
        #region 日志
        private ObservableCollection<ErrorLog> _ErrorLogs = [];

        /// <summary>
        /// 错误Logs
        /// </summary>
        public ObservableCollection<ErrorLog> ErrorLogs
        {
            get { return _ErrorLogs; }
            set { SetProperty(ref _ErrorLogs, value); }
        }
        private TitleState _CurrentState = new();
        /// <summary>
        /// TitleState
        /// </summary>
        public TitleState TitleState
        {
            get { return _CurrentState; }
            set { SetProperty(ref _CurrentState, value); }
        }

        /// <summary>
        /// 添加错误日志
        /// </summary>
        /// <param name="message">消息内容</param>
        public void AddErrorLog(Message message)
        {
            if (TitleState.Info == message.Content)
            {
                TitleState.Background = Brushes.Red;
                return; //避免重复记录相同的错误
            }
            var newLog = new ErrorLog(message.Content);
            TitleState.Info = message.Content;
            TitleState.Background = Brushes.Red;
            _DatasContext.ErrorLogs.Add(newLog);
            ShowLog();

        }
        private readonly object loglock = new();
        private void ShowLog()
        {
            lock (loglock)
            {

                var showLog = _DatasContext.ErrorLogs.Where(e => e.Time.AddDays(1) >= DateTime.Now).OrderByDescending(l => l.Time).Take(30).ToList();

                foreach (var item in showLog)
                {
                    if (ErrorLogs.Contains(item))
                    {
                        continue;
                    }
                    ErrorLogs.Add(item);
                }

                var confirmedLog = ErrorLogs.Where(e => e.IsConfirm).ToList();

                //清理已确认的日志  
                foreach (var item in confirmedLog)
                {
                    ErrorLogs.Remove(item);
                }
            }
        }

        /// <summary>
        /// 错误日志对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        public void ErrorLogDialog(Message message)
        {
            _dialogService.Show("AlertDialog", new DialogParameters($"message={message.Content}"), r =>
            {
                if (r.Result == ButtonResult.Yes)
                {
                    //确认
                    var err = ErrorLogs.FirstOrDefault(e => e.Message == message.Content);
                    if (err != null)
                    {
                        err.IsConfirm = true;
                    }
                }
                else if (r.Result == ButtonResult.No)
                {
                    //取消
                }
            });
        }
        private void ErrMessageReceived(Message message)
        {
            switch (message.Type)
            {
                case ShowType.Record:

                    AddErrorLog(message);
                    break;

                case ShowType.ShowDialog:
                    ErrorLogDialog(message);
                    break;
                case ShowType.ShowDialogAndRecord:
                    AddErrorLog(message);
                    ErrorLogDialog(message);

                    break;
                default:
                    break;
            }

        }
        #endregion
    }
}
