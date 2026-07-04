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
                        if (CurrVF != null)
                        {
                            CurrVF.Flow.Reset();
                            flow.Step++;
                        }
                        else
                        {

                            throw new Exception("视觉功能名称错误!");
                        }
                        break;

                    case 1:
                        if (CurrVF.RunAll(CurrVF.Flow))
                        {
                            flow.Step++;
                        }
                        else if (flow.CheckStepTime(3))
                            throw new Exception("视觉功能超时!");

                        break;

                    case 2:
                        CurrVariable = globalModel.GetVariable("变量名");
                        if (CurrVariable != null)
                        {
                            CurrVariable.Value = 100; // 设置变量值
                            flow.Step++;
                        }
                        else
                        {
                            throw new Exception("机器人名称错误!");

                        }

                        break;
                    case 3:
                        CurrRobot = globalModel.GetRobot("机器人名称");
                        if (CurrRobot != null)
                        {

                            flow.Step++;

                        }
                        else
                        {
                            throw new Exception("机器人名称错误!");
                        }

                        break;
                    case 4:

                        if (CurrRobot.RunPoint("点位名称"))
                        {
                            flow.Step++;
                        }
                        else
                        {
                            throw new Exception("机器人点位运行失败!");
                        }

                        break;


                    case 5:
                        if (CurrRobot.PointDone) // 等待运动完成
                        {
                            flow.Step++;
                        }
                        else if (flow.CheckStepTime(3))
                            throw new Exception("机器人点位运动超时!");
                        break;
                    case 6:

                        if (CurrRobot.Jog("点位名称", "X+", 10))
                        {
                            flow.Step++;
                        }
                        else
                        {
                            throw new Exception("机器人点位运行失败!");
                        }

                        break;


                    case 7:
                        if (CurrRobot.PointDone) // 等待运动完成
                        {
                            flow.Step++;
                        }
                        else if (flow.CheckStepTime(3))
                            throw new Exception("机器人点位运动超时!");
                        break;
                    case 8:
                        //显示状态
                        CurrControl = globalModel.GetCustomControl("控件名称");
                        if (CurrControl != null)
                        {
                            CurrControl.Params.CellInfos[0].State = CellState.完成;
                            flow.Step++;
                        }
                        else {
                            throw new Exception("控件名称错误!");
                        }

                        break;
                }
            }

        }
    }
}
