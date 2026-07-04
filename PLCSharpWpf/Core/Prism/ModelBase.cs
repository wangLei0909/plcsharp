using PLCSharp.Models;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// 模型Base
    /// </summary>
    public class ModelBase : BindableBase
    {
        /// <summary>
        /// _EventAggregator
        /// </summary>
        protected readonly IEventAggregator _EventAggregator;
        /// <summary>
        /// 对话框服务
        /// </summary>
        [NotMapped]
        public readonly IDialogService _dialogService;
        /// <summary>
        /// _container
        /// </summary>
        [NotMapped]
        public readonly IContainerExtension _container;
        /// <summary>
        /// _DatasContext
        /// </summary>
        [NotMapped]
        public DatasContext _DatasContext { get; set; }
        /// <summary>
        /// 模型Base
        /// </summary>
        public ModelBase(IContainerExtension container, IEventAggregator ea, IDialogService dialogService)
        {
            _container = container;
            _DatasContext = container.Resolve<DatasContext>();
            _EventAggregator = ea;
            _dialogService = dialogService;
            AppDomain.CurrentDomain.ProcessExit += OnExit;

        }


        /// <summary>
        /// OnExit
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        protected virtual void OnExit(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// 发送消息到事件总线
        /// </summary>
        /// <param name="msg">消息</param>
        /// <param name="type">消息类型  1 记录 2 弹出 3 弹出并记录，4 弹出确认框</param>
        public void SendErr(string msg, ShowType type, ErrType errType)
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
        public void SendErr(string msg )
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