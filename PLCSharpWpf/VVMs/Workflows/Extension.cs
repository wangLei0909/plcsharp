using PLCSharp.Core.Common;
using PLCSharp.Models;
using PLCSharp.VVMs.Connects;
using PLCSharp.VVMs.GlobalVariables;
using PLCSharp.VVMs.Homepage;
using PLCSharp.VVMs.MotionController;
using PLCSharp.VVMs.Robots;
using PLCSharp.VVMs.Vision;

namespace PLCSharp.VVMs.Workflows
{
    /// <summary>
    /// 脚本代码示例
    /// </summary>
    public class Extension
    {
        private Robot CurrRobot;
        private CustomControl CurrtateControl;
        private VisionFunction CurrVF;
        private Connect CurrConnect;
        private AxisPoint CurrAxisPoint;
        private Variable CurrVariable;
        private CustomControl CurrControl;

        public void Run(GlobalModel globalModel, FlowModel flow)
        {
            if (globalModel.ModeState.State.Execute)
            {
                switch (flow.Step)
                {
                    //视觉功能
                    case 0:
                        CurrVF = globalModel.GetVisionFunction("视觉功能名");
                        CurrVF.Flow.Reset();
                        flow.Step++;

                        break;

                    case 1:
                        if (CurrVF.RunAll(CurrVF.Flow))
                        {
                            flow.Step++;
                        }
                        else if (flow.CheckStepTime(3))
                        {
                            flow.Step--;
                            throw new Exception("视觉功能超时!");
                        }
                        break;
                    //全局变量
                    case 2:
                        CurrVariable = globalModel.GetVariable("变量名");
                        CurrVariable.Value = 100; // 设置变量值
                        flow.Step++;
                        break;
                    //机器人绝对运动
                    case 3:
                        CurrRobot = globalModel.GetRobot("机器人名称");
                        flow.Step++;
                        break;
                    case 4:
                        CurrRobot.RunPoint("点位名称");
                        flow.Step++;
                        break;

                    case 5:
                        if (CurrRobot.PointDone) // 等待运动完成
                        {
                            flow.Step++;
                        }
                        else if (flow.CheckStepTime(3)) // 超时处理
                        {
                            flow.Step--;
                            throw new Exception("机器人点位运动超时!");
                        }
                        break;

                    //机器人相对运动
                    case 6:
                        CurrRobot.Jog("点位名称", "X+", 10);
                        flow.Step++;
                        break;


                    case 7:
                        if (CurrRobot.PointDone) // 等待运动完成
                        {
                            flow.Step++;
                        }
                        else if (flow.CheckStepTime(3))
                        {
                            flow.Step--;
                            throw new Exception("机器人点位运动超时!");
                        }
                        break;
                    case 8:
                        //显示状态
                        CurrControl = globalModel.GetCustomControl("控件名称");
                        if (CurrControl != null)
                        {
                            CurrControl.Params.CellInfos[0].State = CellState.完成;
                            flow.Step++;
                        }
                        else
                        {
                            throw new Exception("控件名称错误!");
                        }

                        break;
                    //网络通信  SocketServer广播 /SocketClient / FreeSerialProtocol
                    case 9:

                        CurrConnect = globalModel.GetConnect("网络连接名");
                        flow.TaskBool = CurrConnect.SendAsync("要发送的消息");
                        flow.Step++;

                        break;
                    case 10:
                        if (flow.TaskBool.IsCompleted)
                        {
                            if (flow.TaskBool.Result)
                            {
                                if (string.IsNullOrEmpty(CurrConnect.ReceiveInfo))
                                {
                                    //这里处理 接收到的消息 CurrConnect.ReceiveInfo
                                    flow.Step++;
                                }
                                else if (flow.CheckStepTime(3)) // 如果不需要判断超时，可以不写这个判断
                                {
                                    flow.Step--;
                                    throw new Exception("回复超时!");
                                }

                            }
                            else
                            {
                                flow.Step--;
                                throw new Exception($"发送失败!{CurrConnect.ErrInfo}");
                            }
                        }
                        else if (flow.CheckStepTime(3))
                        {
                            flow.Step--;
                            throw new Exception("发送超时!");
                        }

                        break;

                    //网络通信 SocketServer 向指定客户端发送消息
                    case 11:
                        CurrConnect = globalModel.GetConnect("网络连接名");
                        flow.TaskBool = CurrConnect.SendAsync("要发送的消息", "客户端名称");
                        flow.Step++;
                        break;

                    case 12:
                        if (flow.TaskBool.IsCompleted)
                        {
                            if (flow.TaskBool.Result)
                            {
                                if (string.IsNullOrEmpty(CurrConnect.ReceiveInfo))
                                {
                                    //这里处理 接收到的消息 CurrConnect.ReceiveInfo
                                    flow.Step++;
                                }
                                else if (flow.CheckStepTime(3)) // 如果不需要判断超时，可以不写这个判断
                                {
                                    flow.Step--;
                                    throw new Exception("回复超时!");
                                }

                            }
                            else
                            {
                                flow.Step--;
                                throw new Exception($"发送失败!{CurrConnect.ErrInfo}");
                            }
                        }
                        else if (flow.CheckStepTime(3))
                        {
                            flow.Step--;
                            throw new Exception("发送超时!");
                        }

                        break;
                }
            }

        }
    }
}
