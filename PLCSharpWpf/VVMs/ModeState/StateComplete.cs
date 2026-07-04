using Prism.Mvvm;

namespace PLCSharp.VVMs.ModeState
{
    /// <summary>
    /// StateComplete
    /// </summary>
    public class StateComplete : BindableBase
    {

        private bool _Reset;
        /// <summary>
        /// 重置
        /// </summary>
        public bool Reset
        {
            get { return _Reset; }
            set { SetProperty(ref _Reset, value); }
        }

        private bool _ResettingSC;
        /// <summary>
        /// ResettingSC
        /// </summary>
        public bool ResettingSC
        {
            get { return _ResettingSC; }
            set { SetProperty(ref _ResettingSC, value); }
        }

        private bool _Start;
        /// <summary>
        /// 启动
        /// </summary>
        public bool Start
        {
            get { return _Start; }
            set { SetProperty(ref _Start, value); }
        }

        private bool _StartingSC;
        /// <summary>
        /// StartingSC
        /// </summary>
        public bool StartingSC
        {
            get { return _StartingSC; }
            set { SetProperty(ref _StartingSC, value); }
        }
        private bool _Stop;
        /// <summary>
        /// 停止
        /// </summary>
        public bool Stop
        {
            get { return _Stop; }
            set { SetProperty(ref _Stop, value); }
        }

        private bool _StoppingSC;
        /// <summary>
        /// StoppingSC
        /// </summary>
        public bool StoppingSC
        {
            get { return _StoppingSC; }
            set { SetProperty(ref _StoppingSC, value); }
        }

        private bool _Hold;
        /// <summary>
        /// Hold
        /// </summary>
        public bool Hold
        {
            get { return _Hold; }
            set { SetProperty(ref _Hold, value); }
        }

        private bool _HoldingSC;
        /// <summary>
        /// HoldingSC
        /// </summary>
        public bool HoldingSC
        {
            get { return _HoldingSC; }
            set { SetProperty(ref _HoldingSC, value); }
        }

        private bool _UnHold;
        /// <summary>
        /// UnHold
        /// </summary>
        public bool UnHold
        {
            get { return _UnHold; }
            set { SetProperty(ref _UnHold, value); }
        }

        private bool _UnHoldingSC;
        /// <summary>
        /// UnHoldingSC
        /// </summary>
        public bool UnHoldingSC
        {
            get { return _UnHoldingSC; }
            set { SetProperty(ref _UnHoldingSC, value); }
        }

        private bool _Suspend;
        /// <summary>
        /// Suspend
        /// </summary>
        public bool Suspend
        {
            get { return _Suspend; }
            set { SetProperty(ref _Suspend, value); }
        }

        private bool _SuspendingSC;
        /// <summary>
        /// SuspendingSC
        /// </summary>
        public bool SuspendingSC
        {
            get { return _SuspendingSC; }
            set { SetProperty(ref _SuspendingSC, value); }
        }

        private bool _UnSuspend;
        /// <summary>
        /// UnSuspend
        /// </summary>
        public bool UnSuspend
        {
            get { return _UnSuspend; }
            set { SetProperty(ref _UnSuspend, value); }
        }

        private bool _UnSuspendingSC;
        /// <summary>
        /// UnSuspendingSC
        /// </summary>
        public bool UnSuspendingSC
        {
            get { return _UnSuspendingSC; }
            set { SetProperty(ref _UnSuspendingSC, value); }
        }

        private bool _Abort;
        /// <summary>
        /// Abort
        /// </summary>
        public bool Abort
        {
            get { return _Abort; }
            set { SetProperty(ref _Abort, value); }
        }

        private bool _AbortingSC;
        /// <summary>
        /// AbortingSC
        /// </summary>
        public bool AbortingSC
        {
            get { return _AbortingSC; }
            set { SetProperty(ref _AbortingSC, value); }
        }

        private bool _Clear;
        /// <summary>
        /// 清空
        /// </summary>
        public bool Clear
        {
            get { return _Clear; }
            set { SetProperty(ref _Clear, value); }
        }

        private bool _ClearingSC;
        /// <summary>
        /// ClearingSC
        /// </summary>
        public bool ClearingSC
        {
            get { return _ClearingSC; }
            set { SetProperty(ref _ClearingSC, value); }
        }

        private bool _ExecuteSC;
        /// <summary>
        /// 执行SC
        /// </summary>
        public bool ExecuteSC
        {
            get { return _ExecuteSC; }
            set { SetProperty(ref _ExecuteSC, value); }
        }

        private bool _CompletingSC;
        /// <summary>
        /// CompletingSC
        /// </summary>
        public bool CompletingSC
        {
            get { return _CompletingSC; }
            set { SetProperty(ref _CompletingSC, value); }
        }

        private bool _CompleteSC;
        /// <summary>
        /// CompleteSC
        /// </summary>
        public bool CompleteSC
        {
            get { return _CompleteSC; }
            set { SetProperty(ref _CompleteSC, value); }
        }
    }
}