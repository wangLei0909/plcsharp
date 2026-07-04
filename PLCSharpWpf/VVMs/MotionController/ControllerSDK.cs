using static PLCSharp.VVMs.MotionController.Axis;
using static PLCSharp.VVMs.MotionController.InterpolationGroup;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// ControllerSDK
    /// </summary>
    public abstract class ControllerSDK
    {
        public abstract bool Init(ushort controllerNo, string ip);
        public abstract void Close();
        public abstract void GetDI();
        public abstract void GetDQ();
        public abstract void SetDQ(ushort port, ushort off_on);
        /// <summary>
        /// DI_数量
        /// </summary>
        public int DI_Count { get; set; }
        /// <summary>
        /// DI
        /// </summary>
        public List<uint> DI { get; set; } = [];
        /// <summary>
        /// DQ_数量
        /// </summary>
        public int DQ_Count { get; set; }
        /// <summary>
        /// DQ
        /// </summary>
        public List<uint> DQ { get; set; } = [];
        public abstract void Get_Axis_Status(ushort axisNo);

        public abstract void Stop(ushort axisNo);
        public abstract void Reset(ushort axisNo);
        public abstract short CreateORG(ushort axisNo, AxisParams axisParams);
        public abstract short Power(ushort axisNo, bool onoff);
        public abstract bool CheckORG(ushort axisNo);
        public abstract short Move(ushort axisNo, ushort mode, AxisParams axisParams);

        public abstract short MulticoorMove(List<Interpolation> interpolations, InterpolationGroupParams interpolationParams);
        public abstract short Save(ushort axisNo, AxisParams axisParams);

        /// <summary>
        /// ControllerNo
        /// </summary>
        public ushort ControllerNo { get; set; }

        /// <summary>
        /// 轴IOs
        /// </summary>
        public List<AxisState> AxisIOs { get; set; } = [];

    }

    /// <summary>
    /// 轴State
    /// </summary>
    public class AxisState
    {
        /// <summary>
        /// PowerOn
        /// </summary>
        public bool PowerOn { get; set; }
        /// <summary>
        /// ALM
        /// </summary>
        public bool ALM { get; set; }
        /// <summary>
        /// ORG
        /// </summary>
        public bool ORG { get; set; }
        /// <summary>
        /// LimitP
        /// </summary>
        public bool LimitP { get; set; }
        /// <summary>
        /// LimitN
        /// </summary>
        public bool LimitN { get; set; }
        /// <summary>
        /// Moving
        /// </summary>
        public bool Moving { get; set; }
        /// <summary>
        /// 指令位置反馈
        /// </summary>
        public double CommandPosition { get; set; }
        /// <summary>
        /// 编码器位置
        /// </summary>
        public double EncoderPosition { get; set; }
        /// <summary>
        /// 速度反馈
        /// </summary>
        public double Velocity { get; set; }
    }

}
