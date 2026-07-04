namespace PLCSharp.Core.Common
{
    /// <summary>
    /// 流程模型
    /// </summary>
    public class FlowModel
    {


        private int _Step;
        /// <summary>
        /// 当前执行的步骤序号
        /// </summary>
        public int Step
        {
            get { return _Step; }
            set
            {
                _Step = value;

                StepTime = DateTime.Now;
            }
        }


        /// <summary>
        /// 当前步骤的开始时间
        /// </summary>
        public DateTime StepTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 整个流程的开始时间
        /// </summary>
        public DateTime FlowStarTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 是否完成
        /// </summary>
        public bool Done { get; set; }
        /// <summary>
        /// 默认构造函数，自动调用 Reset 初始化
        /// </summary>
        public FlowModel()
        {

            Reset();
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            Done = false;
            Step = 0;
            StepTime = DateTime.Now;
            FlowStarTime = DateTime.Now;
        }
        /// <summary>
        /// 检查当前步骤是否已超时
        /// </summary>
        /// <param name="second">超时阈值（秒）</param>
        /// <returns>超时返回 true，否则返回 false</returns>
        public bool CheckStepTime(double second)
        {
            return StepTime.AddSeconds(second) < DateTime.Now;
        }

        /// <summary>
        /// 检查整个流程是否已超时
        /// </summary>
        /// <param name="second">超时阈值（秒）</param>
        /// <returns>超时返回 true，否则返回 false</returns>
        public bool CheckFlowTime(double second)
        {
            return FlowStarTime.AddSeconds(second) < DateTime.Now;
        }

    }
}
