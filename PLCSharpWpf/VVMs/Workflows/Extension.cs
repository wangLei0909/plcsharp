using PLCSharp.Core.Common;
using PLCSharp.Models;
using PLCSharp.VVMs.Connects;
using PLCSharp.VVMs.Homepage;
using PLCSharp.VVMs.MotionController;
using PLCSharp.VVMs.Robots;
using PLCSharp.VVMs.Vision;

namespace PLCSharp.VVMs.Workflows
{
    public class Extension
    {
        Robot CurrRobot;
        CustomControl CurrtateControl;
        VisionFunction CurrVF; 
        Connect CurrConnect;
        AxisPoint CurrAxisPoint;


        public void Run(GlobalModel globalModel, FlowModel flow)
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
                        throw new Exception("视觉功能超时!");

                    break;
            }

        }
    }
}
 