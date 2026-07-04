using Prism.Commands;
using Prism.Mvvm;

namespace PLCSharp.VVMs.ModeState
{
    /// <summary>
    /// 模式与状态
    /// </summary>
    public class ModeState : BindableBase
    {
        /// <summary>
        /// Mode
        /// </summary>
        public Mode Mode { get; set; } = new();

        private WorkMode _ModeEnum;

        /// <summary>
        /// ModeEnum
        /// </summary>
        public WorkMode ModeEnum
        {
            get { return _ModeEnum; }
            private set { SetProperty(ref _ModeEnum, value); }
        }

        /// <summary>
        /// Mc
        /// </summary>
        public ModeChange Mc { get; set; } = new();
        /// <summary>
        /// State
        /// </summary>
        public State State { get; set; } = new();
        private STATE _StateEnum;

        /// <summary>
        /// StateEnum
        /// </summary>
        public STATE StateEnum
        {
            get { return _StateEnum; }
            private set { SetProperty(ref _StateEnum, value); }
        }

        /// <summary>
        /// Sc
        /// </summary>
        public StateComplete Sc { get; set; } = new();
        /// <summary>
        /// State配置
        /// </summary>
        public State StateConfig { get; set; } = new();

        private DelegateCommand<string> _ViewCommand;

        /// <summary>
        /// 视图Command
        /// </summary>
        public DelegateCommand<string> ViewCommand =>
            _ViewCommand ??= new DelegateCommand<string>(ExecuteViewCommand);

        private void ExecuteViewCommand(string cmd)
        {
            switch (cmd)
            {
                case "Start":
                    Sc.Start = true;
                    break;

                case "Stop":
                    Sc.Stop = true;
                    break;

                case "Reset":
                    Sc.Reset = true;
                    break;

                case "Clear":
                    Sc.Clear = true;
                    break;

                case "Abort":
                    Sc.Abort = true;
                    break;

                case "Production":
                    Mc.ProductionMC = true;
                    break;
                case "Manual":
                    Mc.ManualMC = true;
                    break;
                case "Edit":
                    Mc.EditMC = true;
                    break;
            }
        }

        /// <summary>
        /// 运行
        /// </summary>
        public void Run()
        {
            #region 模式

            switch (ModeEnum)
            {
                case WorkMode.Undefined:
                    ModeEnum = WorkMode.Manual;
                    break;

                case WorkMode.Production:
                    //仅在空闲、停止和中止状态下允许更改模式。
                    if (StateEnum == STATE.Stopped
                        || StateEnum == STATE.Idle
                        || StateEnum == STATE.Aborted
                        )
                    {
                        if (Mc.MaintenanceMC) ModeEnum = WorkMode.Maintenance;
                        if (Mc.ManualMC) ModeEnum = WorkMode.Manual;
                        if (Mc.EditMC) ModeEnum = WorkMode.Edit;
                    }
                    break;

                case WorkMode.Maintenance:
                    if (Mc.ProductionMC) ModeEnum = WorkMode.Production;
                    if (Mc.ManualMC) ModeEnum = WorkMode.Manual;
                    if (Mc.EditMC) ModeEnum = WorkMode.Edit;
                    break;

                case WorkMode.Manual:
                    if (Mc.ProductionMC) ModeEnum = WorkMode.Production;
                    if (Mc.MaintenanceMC) ModeEnum = WorkMode.Maintenance;
                    if (Mc.EditMC) ModeEnum = WorkMode.Edit;
                    break;
                case WorkMode.Edit:
                    if (Mc.ProductionMC) ModeEnum = WorkMode.Production;
                    if (Mc.MaintenanceMC) ModeEnum = WorkMode.Maintenance;
                    if (Mc.ManualMC) ModeEnum = WorkMode.Manual;

                    break;

            }

            Mode.Production = ModeEnum == WorkMode.Production;
            Mode.Manual = ModeEnum == WorkMode.Manual;
            Mode.Maintenance = ModeEnum == WorkMode.Maintenance;
            Mode.Edit = ModeEnum == WorkMode.Edit;

            Mc.ProductionMC = false;
            Mc.ManualMC = false;
            Mc.MaintenanceMC = false;
            Mc.EditMC = false;

            #endregion 模式

            #region 状态

            if (StateConfig.Clearing == false) Sc.ClearingSC = true;
            if (StateConfig.Starting == false) Sc.StartingSC = true;
            if (StateConfig.Suspended == false) Sc.UnSuspend = true;
            if (StateConfig.Stopping == false) Sc.StoppingSC = true;
            if (StateConfig.Aborting == false) Sc.AbortingSC = true;
            if (StateConfig.Holding == false) Sc.HoldingSC = true;
            if (StateConfig.Held == false) Sc.UnHold = true;
            if (StateConfig.UnHolding == false) Sc.UnHoldingSC = true;
            if (StateConfig.Suspending == false) Sc.SuspendingSC = true;
            if (StateConfig.Resetting == false) Sc.ResettingSC = true;
            if (StateConfig.Completing == false) Sc.CompletingSC = true;
            if (StateConfig.Complete == false) Sc.CompleteSC = true;


            switch (StateEnum)
            {
                case STATE.Clearing:            // 1
                    if (Sc.ClearingSC) StateEnum = STATE.Stopped;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Stopped:             // 2
                    if (Sc.Reset) StateEnum = STATE.Resetting;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Starting:            // 3
                    if (Sc.StartingSC) StateEnum = STATE.Execute;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Idle:                // 4
                    //只有在生产模式下才能启动
                    if (ModeEnum == WorkMode.Production)
                    {
                        if (Sc.Start) StateEnum = STATE.Starting;
                    }
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Suspended:           // 5
                    if (Sc.UnSuspend) StateEnum = STATE.UnSuspending;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Execute:             // 6
                    if (Sc.ExecuteSC) StateEnum = STATE.Completing;
                    if (Sc.Hold) StateEnum = STATE.Holding;
                    if (Sc.Suspend) StateEnum = STATE.Suspending;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Stopping:            // 7
                    if (Sc.StoppingSC) StateEnum = STATE.Stopped;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Aborting:            // 8
                    if (Sc.AbortingSC) StateEnum = STATE.Aborted;
                    break;

                case STATE.Aborted:             // 9
                    if (Sc.Clear) StateEnum = STATE.Clearing;
                    break;

                case STATE.Holding:             // 10
                    if (Sc.HoldingSC) StateEnum = STATE.Held;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Held:                // 11
                    if (Sc.UnHold) StateEnum = STATE.UnHolding;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.UnHolding:           // 12
                    if (Sc.UnHoldingSC) StateEnum = STATE.Execute;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Suspending:          // 13
                    if (Sc.SuspendingSC) StateEnum = STATE.Suspended;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.UnSuspending:        // 14
                    if (Sc.UnSuspendingSC) StateEnum = STATE.Execute;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Resetting:           // 15
                    if (Sc.ResettingSC) StateEnum = STATE.Idle;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Completing:          // 16
                    if (Sc.CompletingSC) StateEnum = STATE.Complete;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                case STATE.Complete:            // 17
                    if (Sc.Reset) StateEnum = STATE.Resetting;
                    if (Sc.Stop) StateEnum = STATE.Stopping;
                    if (Sc.Abort) StateEnum = STATE.Aborting;
                    break;

                default:
                    StateEnum = STATE.Aborted;
                    break;
            }

            State.Clearing = StateEnum == STATE.Clearing;
            State.Stopped = StateEnum == STATE.Stopped;
            State.Starting = StateEnum == STATE.Starting;
            State.Idle = StateEnum == STATE.Idle;
            State.Suspended = StateEnum == STATE.Suspended;
            State.Execute = StateEnum == STATE.Execute;
            State.Stopping = StateEnum == STATE.Stopping;
            State.Aborting = StateEnum == STATE.Aborting;
            State.Aborted = StateEnum == STATE.Aborted;
            State.Holding = StateEnum == STATE.Holding;
            State.Held = StateEnum == STATE.Held;
            State.UnHolding = StateEnum == STATE.UnHolding;
            State.Suspending = StateEnum == STATE.Suspending;
            State.UnSuspending = StateEnum == STATE.UnSuspending;
            State.Resetting = StateEnum == STATE.Resetting;
            State.Completing = StateEnum == STATE.Completing;
            State.Complete = StateEnum == STATE.Complete;

            Sc.Reset = false;
            Sc.ResettingSC = false;
            Sc.Start = false;
            Sc.StartingSC = false;
            Sc.Stop = false;
            Sc.StoppingSC = false;
            Sc.Hold = false;
            Sc.HoldingSC = false;
            Sc.UnHold = false;
            Sc.UnHoldingSC = false;
            Sc.Suspend = false;
            Sc.SuspendingSC = false;
            Sc.UnSuspend = false;
            Sc.UnSuspendingSC = false;
            Sc.Abort = false;
            Sc.AbortingSC = false;
            Sc.Clear = false;
            Sc.ClearingSC = false;
            Sc.ExecuteSC = false;
            Sc.CompletingSC = false;
            Sc.CompleteSC = false;

            #endregion 状态
        }
    }

    /// <summary>
    /// State
    /// </summary>
    public class State : BindableBase
    {
        private bool _Undefined;

        /// <summary>
        /// 未定义
        /// </summary>
        public bool Undefined
        {
            get { return _Undefined; }
            set { SetProperty(ref _Undefined, value); }
        }

        private bool _Clearing;

        /// <summary>
        /// 清除中
        /// </summary>
        public bool Clearing
        {
            get { return _Clearing; }
            set { SetProperty(ref _Clearing, value); }
        }

        private bool _Stopped;

        /// <summary>
        /// 停止
        /// </summary>
        public bool Stopped
        {
            get { return _Stopped; }
            set { SetProperty(ref _Stopped, value); }
        }

        private bool _Starting;

        /// <summary>
        /// 正在启动
        /// </summary>
        public bool Starting
        {
            get { return _Starting; }
            set { SetProperty(ref _Starting, value); }
        }

        private bool _Idle;

        /// <summary>
        /// 空闲
        /// </summary>
        public bool Idle
        {
            get { return _Idle; }
            set { SetProperty(ref _Idle, value); }
        }

        private bool _Suspended;

        /// <summary>
        /// 外部暂停
        /// </summary>
        public bool Suspended
        {
            get { return _Suspended; }
            set { SetProperty(ref _Suspended, value); }
        }

        private bool _Execute;

        /// <summary>
        /// 执行
        /// </summary>
        public bool Execute
        {
            get { return _Execute; }
            set { SetProperty(ref _Execute, value); }
        }

        private bool _Stopping;

        /// <summary>
        /// 停止中
        /// </summary>
        public bool Stopping
        {
            get { return _Stopping; }
            set { SetProperty(ref _Stopping, value); }
        }

        private bool _Aborting;

        /// <summary>
        /// 正在中止
        /// </summary>
        public bool Aborting
        {
            get { return _Aborting; }
            set { SetProperty(ref _Aborting, value); }
        }

        private bool _Aborted;

        /// <summary>
        /// 中止
        /// </summary>
        public bool Aborted
        {
            get { return _Aborted; }
            set { SetProperty(ref _Aborted, value); }
        }

        private bool _Holding;

        /// <summary>
        /// 准备内部暂停
        /// </summary>
        public bool Holding
        {
            get { return _Holding; }
            set { SetProperty(ref _Holding, value); }
        }

        private bool _Held;

        /// <summary>
        /// 内部暂停
        /// </summary>
        public bool Held
        {
            get { return _Held; }
            set { SetProperty(ref _Held, value); }
        }

        private bool _UnHolding;

        /// <summary>
        /// 内部暂停解除中
        /// </summary>
        public bool UnHolding
        {
            get { return _UnHolding; }
            set { SetProperty(ref _UnHolding, value); }
        }

        private bool _Suspending;

        /// <summary>
        /// 外部暂停进入中
        /// </summary>
        public bool Suspending
        {
            get { return _Suspending; }
            set { SetProperty(ref _Suspending, value); }
        }

        private bool _UnSuspending;

        /// <summary>
        /// 外部暂停解除中
        /// </summary>
        public bool UnSuspending
        {
            get { return _UnSuspending; }
            set { SetProperty(ref _UnSuspending, value); }
        }

        private bool _Resetting;

        /// <summary>
        /// 复位中
        /// </summary>
        public bool Resetting
        {
            get { return _Resetting; }
            set { SetProperty(ref _Resetting, value); }
        }

        private bool _Completing;

        /// <summary>
        /// 正在完成
        /// </summary>
        public bool Completing
        {
            get { return _Completing; }
            set { SetProperty(ref _Completing, value); }
        }

        private bool _Complete;

        /// <summary>
        /// 完成
        /// </summary>
        public bool Complete
        {
            get { return _Complete; }
            set { SetProperty(ref _Complete, value); }
        }
    }

    /// <summary>
    /// Mode
    /// </summary>
    public class Mode : BindableBase
    {
        private bool _Manual;

        /// <summary>
        /// 手动模式
        /// </summary>
        public bool Manual
        {
            get { return _Manual; }
            set { SetProperty(ref _Manual, value); }
        }

        private bool _Maintenance;

        /// <summary>
        /// 维修模式
        /// </summary>
        public bool Maintenance
        {
            get { return _Maintenance; }
            set { SetProperty(ref _Maintenance, value); }
        }

        private bool _Production;

        /// <summary>
        /// 生产模式
        /// </summary>
        public bool Production
        {
            get { return _Production; }
            set { SetProperty(ref _Production, value); }
        }

        private bool _Edit;
        /// <summary>
        /// 配置项
        /// </summary>
        public bool Edit
        {
            get { return _Edit; }
            set { SetProperty(ref _Edit, value); }
        }
    }
}