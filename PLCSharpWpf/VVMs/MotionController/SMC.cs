using PLCSharp.Core.Tools;
using PLCSharp.SDK.MotionControl.SMC;
using System.Threading;
using static PLCSharp.VVMs.MotionController.Axis;
using static PLCSharp.VVMs.MotionController.InterpolationGroup;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// SMC
    /// </summary>
    public class SMC : ControllerSDK
    {
        /// <summary>
        /// 关闭
        /// </summary>
        public override void Close()
        {
            LTSMC.smc_board_close(ControllerNo);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="controllerNo">controllerNo</param>
        /// <param name="ip">ip</param>
        /// <returns>返回布尔值</returns>
        public override bool Init(ushort controllerNo, string ip)
        {
            ControllerNo = controllerNo;
            var done = LTSMC.smc_board_init(controllerNo, 2, ip, 115200);
            if (done == 0)
            {
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
            var io = LTSMC.smc_axis_io_status(ControllerNo, axisNo);

            AxisIOs[axisNo].ALM = io.GetBit(0);
            AxisIOs[axisNo].LimitP = io.GetBit(1);
            AxisIOs[axisNo].LimitN = io.GetBit(2);
            AxisIOs[axisNo].ORG = io.GetBit(4);
            AxisIOs[axisNo].Moving = LTSMC.smc_check_done(ControllerNo, axisNo) == 0;
            AxisIOs[axisNo].PowerOn = LTSMC.smc_read_sevon_pin(ControllerNo, axisNo) == 0;

            double pos = 0;
            LTSMC.smc_get_position_unit(ControllerNo, axisNo, ref pos);
            AxisIOs[axisNo].CommandPosition = pos;
            double vel = 0;
            LTSMC.smc_read_current_speed_unit(ControllerNo, axisNo, ref vel);
            AxisIOs[axisNo].Velocity = vel;
            double encoder = 0;
            LTSMC.smc_get_encoder_unit(ControllerNo, axisNo, ref encoder);
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
                var dq = LTSMC.smc_read_inport(ControllerNo, (ushort)i);
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
                var dq = LTSMC.smc_read_outport(ControllerNo, (ushort)i);
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
            LTSMC.smc_write_outbit(ControllerNo, port, off_on);
        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="axisNo">轴No</param>
        public override void Stop(ushort axisNo)
        {
            LTSMC.smc_stop(ControllerNo, axisNo, 0);
        }
        /// <summary>
        /// 重置
        /// </summary>
        /// <param name="axisNo">轴No</param>
        public override void Reset(ushort axisNo)
        {
            _ = Task.Run(() =>
            {
                LTSMC.smc_write_erc_pin(ControllerNo, axisNo, 0);
                Thread.Sleep(1000);
                LTSMC.smc_write_erc_pin(ControllerNo, axisNo, 1);
            });

        }

        /// <summary>
        /// Power
        /// </summary>
        /// <param name="axisNo">轴No</param>
        /// <param name="onoff">onoff</param>
        /// <returns>返回 short</returns>
        public override short Power(ushort axisNo, bool onoff)
        {


            if (onoff)
                return LTSMC.smc_write_sevon_pin(ControllerNo, axisNo, 0);
            else
                return LTSMC.smc_write_sevon_pin(ControllerNo, axisNo, 1);

        }

        /// <summary>
        /// 创建ORG
        /// </summary>
        /// <param name="axisNo">轴No</param>
        /// <param name="axisParams">轴Params</param>
        /// <returns>返回 short</returns>
        public override short CreateORG(ushort axisNo, AxisParams axisParams)
        {
            //第一步、设置回原点电平参数
            var ret = LTSMC.smc_set_home_pin_logic(ControllerNo, axisNo, axisParams.OrgLogic, 0);
            if (ret != 0) { return ret; }
            //第二步、设置回原点模式
            ret = LTSMC.smc_set_homemode(ControllerNo, axisNo, axisParams.HomeDir, 1, axisParams.HomeMode, 0);
            if (ret != 0) { return ret; }
            //第三步、设置回原点完成后计数位置值 
            ret = LTSMC.smc_set_home_position_unit(ControllerNo, axisNo, 1, 0);
            if (ret != 0) { return ret; }
            //第四步、设置回原点运动速度参数
            ret = LTSMC.smc_set_home_profile_unit(ControllerNo, axisNo,
                axisParams.HomeLowVelocity,
                axisParams.HomeHighVelocity,
                axisParams.Acc, axisParams.Dec);
            if (ret != 0) { return ret; }
            //第五步、启动回原点运动
            return LTSMC.smc_home_move(ControllerNo, axisNo);
        }

        /// <summary>
        /// 检查ORG
        /// </summary>
        /// <param name="axisNo">轴No</param>
        /// <returns>返回布尔值</returns>
        public override bool CheckORG(ushort axisNo)
        {
            ushort state = 0;
            LTSMC.smc_get_home_result(ControllerNo, axisNo, ref state);
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
            var ret = LTSMC.smc_set_profile_unit(ControllerNo, axisNo,
                    axisParams.MinVelocity,
                    maxVel,
                    axisParams.Acc,
                    axisParams.Dec,
                    axisParams.StopVelocity
                    );
            if (ret != 0) { return ret; }
            if (mode == 0)
            {
                ret = LTSMC.smc_pmove_unit(ControllerNo, axisNo, axisParams.TargetDistance, mode);
            }
            else
            {
                ret = LTSMC.smc_pmove_unit(ControllerNo, axisNo, axisParams.TargetPos, mode);
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
            var ret = LTSMC.smc_set_vector_profile_unit(ControllerNo,
                interpolationParams.Coordinate,
                interpolationParams.MinVelocity,
                interpolationParams.MaxVelocity,
                interpolationParams.Acc,
                interpolationParams.Dec,
                interpolationParams.StopVelocity);
            if (ret != 0) { return ret; }
            ret = LTSMC.smc_set_vector_s_profile(ControllerNo, interpolationParams.Coordinate, 0, 0);
            if (ret != 0) { return ret; }
            ret = LTSMC.smc_conti_set_blend(ControllerNo, interpolationParams.Coordinate, 0);
            if (ret != 0) { return ret; }
            ret = LTSMC.smc_stop_multicoor(ControllerNo, interpolationParams.Coordinate, 1);
            if (ret != 0) { return ret; }

            //打开连续插补缓冲区
            ret = LTSMC.smc_conti_open_list(ControllerNo, interpolationParams.Coordinate, 2,
                [interpolationParams.AxisXNo, interpolationParams.AxisYNo]);
            if (ret != 0) { return ret; }
            //延迟输出
            ret = LTSMC.smc_conti_delay_outbit_to_start(ControllerNo, interpolationParams.Coordinate,
                interpolationParams.OutBit,
                 0,
                 interpolationParams.DelayValue, 1, 0);
            if (ret != 0) { return ret; }
            //提前关闭输出
            ret = LTSMC.smc_conti_ahead_outbit_to_stop(ControllerNo,
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
                        ret = LTSMC.smc_conti_line_unit(ControllerNo,
                            interpolationParams.Coordinate,
                            2,
                            [interpolationParams.AxisXNo, interpolationParams.AxisYNo],
                            [item.InterpolationPoints[0].X, item.InterpolationPoints[0].Y],
                            item.Params.PositionMode,
                            0);
                        break;
                    case InterpolationType.Arc圆弧:
                        ret = LTSMC.smc_conti_arc_move_center_unit(ControllerNo,
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
            ret = LTSMC.smc_conti_start_list(ControllerNo,
                interpolationParams.Coordinate);
            if (ret != 0) { return ret; }
            ret = LTSMC.smc_conti_close_list(ControllerNo,
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
            var ret = LTSMC.smc_set_equiv(ControllerNo, axisNo, axisParams.Equiv);
            if (ret != 0) { return ret; }
            ret = LTSMC.smc_set_pulse_outmode(ControllerNo, axisNo, axisParams.PulseOutMode);
            if (ret != 0) { return ret; }
            ushort el_enable = 0;
            if (axisParams.ForwardLimitEnable == 1 && axisParams.BackwardLimitEnable == 1)
            {
                el_enable = 1;
            }
            else if (axisParams.ForwardLimitEnable == 0 && axisParams.BackwardLimitEnable == 1)
            {
                el_enable = 2;
            }
            else if (axisParams.ForwardLimitEnable == 1 && axisParams.BackwardLimitEnable == 0)
            {
                el_enable = 3;
            }

            ushort el_logic = 0;
            if (axisParams.ForwardLimitLogic == 1 && axisParams.BackwardLimitLogic == 1)
            {
                el_logic = 1;
            }
            else if (axisParams.ForwardLimitLogic == 0 && axisParams.BackwardLimitLogic == 1)
            {
                el_logic = 2;
            }
            else if (axisParams.ForwardLimitLogic == 1 && axisParams.BackwardLimitLogic == 0)
            {
                el_logic = 3;
            }

            ret = LTSMC.smc_set_el_mode(ControllerNo, axisNo, el_enable, el_logic, 0);

            if (ret != 0) { return ret; }
            ret = LTSMC.smc_set_home_pin_logic(ControllerNo, axisNo, axisParams.OrgLogic, 0);
            if (ret != 0) { return ret; }


            ret = LTSMC.smc_set_alm_mode(ControllerNo, axisNo, axisParams.ALMEnable, axisParams.ALMLogic, 0);
            return ret;

        }
    }

}
