namespace PLCSharp.VVMs.ModeState
{
    public enum STATE
    {
        Undefined = 0,         //    未定义
        Clearing = 1,          //    正在清除
        Stopped = 2,           //    已停止
        Starting = 3,          //    正在启动
        Idle = 4,              //    空闲
        Suspended = 5,         //    外部暂停
        Execute = 6,           //    执行
        Stopping = 7,          //    正在停止
        Aborting = 8,          //    正在中止
        Aborted = 9,           //    已中止
        Holding = 10,          //    正在进入内部暂停
        Held = 11,             //    内部暂停
        UnHolding = 12,        //    正在解除内部暂停
        Suspending = 13,       //    正在进入外部暂停
        UnSuspending = 14,     //    正在解除外部暂停
        Resetting = 15,        //    正在复位
        Completing = 16,       //    正在完成
        Complete = 17          //    已完成
    }
}