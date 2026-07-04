using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;

namespace PLCSharp.Core.Common
{
    /// <summary>
    /// 错误日志
    /// </summary>
    public class ErrorLog : BindableBase
    {

        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 错误日志
        /// </summary>
        public ErrorLog(string message)
        {
            Message = message;
            Time = DateTime.Now;

        }

        private string _Message;
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message
        {
            get { return _Message; }
            set { SetProperty(ref _Message, value); }
        }

        private DateTime _Time;
        /// <summary>
        /// Time
        /// </summary>
        public DateTime Time
        {
            get { return _Time; }
            set { SetProperty(ref _Time, value); }
        }

        private bool _IsConfirm;
        /// <summary>
        /// IsConfirm
        /// </summary>
        public bool IsConfirm
        {
            get { return _IsConfirm; }
            set { SetProperty(ref _IsConfirm, value); }
        }
    }
}
