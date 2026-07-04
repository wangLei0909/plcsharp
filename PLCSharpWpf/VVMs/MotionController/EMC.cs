using PLCSharp.Core.Tools;
using PLCSharp.SDK.MotionControl.EMC;
using static PLCSharp.VVMs.MotionController.Axis;
using static PLCSharp.VVMs.MotionController.InterpolationGroup;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// EMC
    /// </summary>
    public class EMC : ControllerSDK
    {
        /// <summary>
        /// 关闭
        /// </summary>
        public override void Close()
        {
            LTDMC.dmc_board_close();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="controllerNo">controllerNo</param>
        /// <param name="ip">ip</param>
        /// <returns>返回布尔值</returns>
        public override bool Init(ushort controllerNo, string ip)
        {

            var done = LTDMC.dmc_board_init_eth(controllerNo, ip);
            if (done == 0)
            {
                ControllerNo = controllerNo;
                return true;
            }
            else
            {
                return false;

            }
        }

        /// <summary>
        /// Get_Axis_状态
        /// </summary>
        /// <param name="axisNo">轴No</param>
        public override void Get_Axis_Status(ushort axisNo)
        {
            while (AxisIOs.Count <= axisNo)
            {
                AxisIOs.Add(new AxisState());
            }
            var io = LTDMC.dmc_axis_io_status(ControllerNo, axisNo);
            ushort statemachine = 0;
            AxisIOs[axisNo].ALM = io.GetBit(0);
            AxisIOs[axisNo].LimitP = io.GetBit(1);
            AxisIOs[axisNo].LimitN = io.GetBit(2);
            AxisIOs[axisNo].ORG = io.GetBit(4);
            AxisIOs[axisNo].Moving = LTDMC.dmc_check_done(ControllerNo, axisNo) == 0;
            LTDMC.nmc_get_axis_state_machine(ControllerNo, axisNo, ref statemachine);
            AxisIOs[axisNo].PowerOn = statemachine == 4;

            double pos = 0;
            LTDMC.dmc_get_position_unit(ControllerNo, axisNo, ref pos);
            AxisIOs[axisNo].CommandPosition = pos;
            double vel = 0;
            LTDMC.dmc_read_current_speed_unit(ControllerNo, axisNo, ref vel);
            AxisIOs[axisNo].Velocity = vel;
            double encoder = 0;
            LTDMC.dmc_get_encoder_unit(ControllerNo, axisNo, ref encoder);
            AxisIOs[axisNo].EncoderPosition = encoder;

        }

        /// <summary>
        /// 获取DI
        /// </summary>
        public override void GetDI()
        {

            var group = DI_Count / 32;

            if (DI_Count % 32 > 0)
            {
                group++;
            }

            for (int i = 0; i < group; i++)
            {
                var dq = LTDMC.dmc_read_inport(ControllerNo, (ushort)i);
                if (DI.Count > i)
                {
                    DI[i] = dq;
                }
                else
                {
                    DI.Add(dq);
                }
            }
        }

        /// <summary>
        /// 获取DQ
        /// </summary>
        public override void GetDQ()
        {

            var group = DQ_Count / 32;

            if (DQ_Count % 32 > 0)
            {
                group++;
            }

            for (int i = 0; i < group; i++)
            {
                var dq = LTDMC.dmc_read_outport(ControllerNo, (ushort)i);
                if (DQ.Count > i)
                {
                    DQ[i] = dq;
                }
                else
                {
                    DQ.Add(dq);
                }
            }
        }

        /// <summary>
        /// 设置DQ
        /// </summary>
        /// <param name="port">端口</param>
        /// <param name="off_on">off_on</param>
        public override void SetDQ(ushort port, ushort off_on)
        {
            LTDMC.dmc_write_outbit(ControllerNo, port, off_on);
        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="axisNo">轴No</param>
        public override void Stop(ushort axisNo)
        {
            LTDMC.dmc_stop(ControllerNo, axisNo, 0);
        }

        /// <summary>
        /// 重置
        /// </summary>
        /// <param name="axisNo">轴No</param>
        public override void Reset(ushort axisNo)
        {


            ushort errcode = 0;
            LTDMC.nmc_get_errcode(ControllerNo, 2, ref errcode); //获取总线状态
            if (errcode != 0)
            {

                LTDMC.nmc_clear_errcode(ControllerNo, 2);//尝试清除总线错误

            }
            else
            {

                LTDMC.nmc_driver_reset(ControllerNo, axisNo);

            }

        }

        /// <summary>
        /// 创建ORG
        /// </summary>
        /// <param name="axisNo">轴No</param>
        /// <param name="axisParams">轴Params</param>
        /// <returns>返回 short</returns>
        public override short CreateORG(ushort axisNo, AxisParams axisParams)
        {
            ushort errcode = 1;
            LTDMC.nmc_get_errcode(ControllerNo, axisNo, ref errcode); //获取总线状态
            if (errcode == 0) //总线正常 
            {
                // 设置回原点运动速度参数
                var ret = LTDMC.nmc_set_home_profile(ControllerNo, axisNo,
                      axisParams.HomeMode,
                      axisParams.HomeLowVelocity,
                      axisParams.HomeHighVelocity,
                      axisParams.Acc,
                      axisParams.Dec,
                     0
                      );
                if (ret != 0) { return ret; }
                return LTDMC.nmc_home_move(ControllerNo, axisNo);
            }
            else
            {
                return (short)errcode;
            }
        }

        /// <summary>
        /// Power
        /// </summary>
        /// <param name="axisNo">轴No</param>
        /// <param name="onoff">onoff</param>
        /// <returns>返回 short</returns>
        public override short Power(ushort axisNo, bool onoff)
        {
            ushort errcode = 1;
            LTDMC.nmc_get_errcode(ControllerNo, axisNo, ref errcode); //获取总线状态
            if (errcode == 0)
                if (onoff)
                    return LTDMC.nmc_set_axis_enable(ControllerNo, axisNo);
                else
                    return LTDMC.nmc_set_axis_disable(ControllerNo, axisNo);
            else
            {
                return (short)errcode;
            }
        }

        /// <summary>
        /// 检查ORG
        /// </summary>
        /// <param name="axisNo">轴No</param>
        /// <returns>返回布尔值</returns>
        public override bool CheckORG(ushort axisNo)
        {
            ushort state = 0;
            LTDMC.dmc_get_home_result(ControllerNo, axisNo, ref state);
            return state == 1;
        }

        /// <summary>
        /// 移动
        /// </summary>
        /// <param name="axisNo">轴No</param>
        /// <param name="mode">mode</param>
        /// <param name="axisParams">轴Params</param>
        /// <returns>返回 short</returns>
        public override short Move(ushort axisNo, ushort mode, AxisParams axisParams)
        {
            var maxVel = axisParams.MaxVelocity * axisParams.Rate / 100;
            var ret = LTDMC.dmc_set_profile_unit(ControllerNo, axisNo,
                    axisParams.MinVelocity,
                    maxVel,
                    axisParams.Acc,
                    axisParams.Dec,
                    axisParams.StopVelocity
                    );
            if (ret != 0) { return ret; }
            if (mode == 0)
            {
                ret = LTDMC.dmc_pmove_unit(ControllerNo, axisNo, axisParams.TargetDistance, mode);
            }
            else
            {
                ret = LTDMC.dmc_pmove_unit(ControllerNo, axisNo, axisParams.TargetPos, mode);
            }
            return ret;
        }

        /// <summary>
        /// Multicoor移动
        /// </summary>
        /// <param name="interpolations">interpolations</param>
        /// <param name="interpolationParams">插补Params</param>
        /// <returns>返回 short</returns>
        public override short MulticoorMove(List<Interpolation> interpolations, InterpolationGroupParams interpolationParams)
        {
            var ret = LTDMC.dmc_set_vector_profile_unit(ControllerNo,
                interpolationParams.Coordinate,
                interpolationParams.MinVelocity,
                interpolationParams.MaxVelocity,
                interpolationParams.Acc,
                interpolationParams.Dec,
                interpolationParams.StopVelocity);
            if (ret != 0) { return ret; }
            ret = LTDMC.dmc_set_vector_s_profile(ControllerNo, interpolationParams.Coordinate, 0, 0);
            if (ret != 0) { return ret; }
            ret = LTDMC.dmc_conti_set_blend(ControllerNo, interpolationParams.Coordinate, 0);
            if (ret != 0) { return ret; }
            ret = LTDMC.dmc_stop_multicoor(ControllerNo, interpolationParams.Coordinate, 1);
            if (ret != 0) { return ret; }

            //打开连续插补缓冲区
            ret = LTDMC.dmc_conti_open_list(ControllerNo, interpolationParams.Coordinate, 2,
                [interpolationParams.AxisXNo, interpolationParams.AxisYNo]);
            if (ret != 0) { return ret; }
            //延迟输出
            ret = LTDMC.dmc_conti_delay_outbit_to_start(ControllerNo, interpolationParams.Coordinate,
                interpolationParams.OutBit,
                 0,
                 interpolationParams.DelayValue, 1, 0);
            if (ret != 0) { return ret; }
            //提前关闭输出
            ret = LTDMC.dmc_conti_ahead_outbit_to_stop(ControllerNo,
                interpolationParams.Coordinate,
                0,
                1,
                interpolationParams.Ahead,
                1,
                0);
            if (ret != 0) { return ret; }

            foreach (var item in interpolations)
            {
                switch (item.Type)
                {
                    case InterpolationType.Line直线:
                        ret = LTDMC.dmc_conti_line_unit(ControllerNo,
                            interpolationParams.Coordinate,
                            2,
                            [interpolationParams.AxisXNo, interpolationParams.AxisYNo],
                            [item.InterpolationPoints[0].X, item.InterpolationPoints[0].Y],
                            item.Params.PositionMode,
                            0);
                        break;
                    case InterpolationType.Arc圆弧:
                        ret = LTDMC.dmc_conti_arc_move_center_unit(ControllerNo,
                        interpolationParams.Coordinate, 2,
                        [interpolationParams.AxisXNo, interpolationParams.AxisYNo],
                         [item.InterpolationPoints[1].X, item.InterpolationPoints[1].Y],
                          [item.InterpolationPoints[0].X, item.InterpolationPoints[0].Y],
                         item.Params.Dir,
                         0,
                         item.Params.PositionMode,
                         0);
                        break;
                    default:
                        break;
                }

            }
            ret = LTDMC.dmc_conti_start_list(ControllerNo,
                interpolationParams.Coordinate);
            if (ret != 0) { return ret; }
            ret = LTDMC.dmc_conti_close_list(ControllerNo,
                interpolationParams.Coordinate);

            return ret;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="axisNo">轴No</param>
        /// <param name="axisParams">轴Params</param>
        /// <returns>返回 short</returns>
        public override short Save(ushort axisNo, AxisParams axisParams)
        {
            var ret = LTDMC.dmc_set_equiv(ControllerNo, axisNo, axisParams.Equiv);
            if (ret != 0) { return ret; }


            return ret;
        }
    }

}
