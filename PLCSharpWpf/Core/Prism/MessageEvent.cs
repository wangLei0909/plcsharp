using Prism.Events;

namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// MessageEvent
    /// </summary>
    public class MessageEvent : PubSubEvent<Message>
    {
    }

    /// <summary>
    /// 消息内容
    /// </summary>
    public struct Message
    {
        /// <summary>
        /// 消息目标
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 消息类型  1 记录 2 弹出  3弹出并记录
        /// </summary>
        public ShowType Type { get; set; }


         public ErrType ErrType { get; set; }
    }

    /// <summary>
    /// BytesEvent
    /// </summary>
    public class BytesEvent : PubSubEvent<BytesEventData>
    {
    }

    /// <summary>
    /// BytesEvent数据
    /// </summary>
    public struct BytesEventData
    {
        /// <summary>
        /// 事件目标
        /// </summary>
        public string EventTarget { get; set; }

        /// <summary>
        /// 数据目标
        /// </summary>
        public string DataTarget { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public byte[] Data { get; set; }
    }

    public enum ShowType
    {


        /// <summary>
        /// 记录
        /// </summary>
        Record = 1,
        /// <summary>
        /// 弹出
        /// </summary>
        ShowDialog = 2,
        /// <summary>
        /// 弹出并记录
        /// </summary>
        ShowDialogAndRecord = 3,
    }

    public enum ErrType
    {


        /// <summary>
        /// 普通信息
        /// </summary>
        Info = 1,
        /// <summary>
        /// 故障
        /// </summary>
        Error = 2,
 
    }
}