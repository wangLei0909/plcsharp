using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    public class TwoLineIntersectHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.两线交点;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var line1Name = item.StringParams.TryGetValue("Line1Var", out var l1) && !string.IsNullOrEmpty(l1) ? l1 : "None";
            var line2Name = item.StringParams.TryGetValue("Line2Var", out var l2) && !string.IsNullOrEmpty(l2) ? l2 : "None";

            var line1Var = func.Params.Variables.FirstOrDefault(v => v.Name == line1Name);
            var line2Var = func.Params.Variables.FirstOrDefault(v => v.Name == line2Name);

            if (line1Var == null) throw new Exception($"线1变量 '{line1Name}' 不存在！");
            if (line2Var == null) throw new Exception($"线2变量 '{line2Name}' 不存在！");

            if (line1Var.RawValue is not Line line1 || line2Var.RawValue is not Line line2)
                throw new Exception("变量类型必须是 Line！");

            var ld1 = new Line2D(line1.From.X, line1.From.Y, line1.To.X, line1.To.Y);
            var ld2 = new Line2D(line2.From.X, line2.From.Y, line2.To.X, line2.To.Y);

            MatExtension.IntersectionPoint(ld1, ld2, out var cross);

            // 计算夹角
            double vx1 = line1.To.X - line1.From.X, vy1 = line1.To.Y - line1.From.Y;
            double vx2 = line2.To.X - line2.From.X, vy2 = line2.To.Y - line2.From.Y;
            double a1 = Math.Atan2(vy1, vx1), a2 = Math.Atan2(vy2, vx2);
            double line1Angle = Math.Round(a1 * 180 / Math.PI, 3);
            double intersectAngle = Math.Round(Math.Abs(a2 - a1) * 180 / Math.PI, 3);
            if (intersectAngle > 180) intersectAngle = 360 - intersectAngle;

            double ix = Math.Round(cross.X, 3);
            double iy = Math.Round(cross.Y, 3);
            func.Params.ResultDoubles["IntersectX"] = ix;
            func.Params.ResultDoubles["IntersectY"] = iy;
            func.Params.ResultDoubles["IntersectAngle"] = intersectAngle;

            // 模板模式
            if (item.StringParams.TryGetValue("ComputeTemplate", out var ct) && ct == "1")
            {
                item.DoubleParams["TemplateIntersectX"] = ix;
                item.DoubleParams["TemplateIntersectY"] = iy;
                item.DoubleParams["TemplateLineAngle"] = line1Angle;
                item.Flow.Done = true;
                return true;
            }

            // 有模板时计算偏移
            if (item.DoubleParams.TryGetValue("TemplateIntersectX", out double tix) &&
                item.DoubleParams.TryGetValue("TemplateIntersectY", out double tiy))
            {
                double offX = Math.Round(ix - tix, 3);
                double offY = Math.Round(iy - tiy, 3);
                double offAngle = 0;
                if (item.DoubleParams.TryGetValue("TemplateLineAngle", out double ta))
                    offAngle = Math.Round(line1Angle - ta, 3);
                func.Params.ResultDoubles["IntersectOffX"] = offX;
                func.Params.ResultDoubles["IntersectOffY"] = offY;
                func.Params.ResultDoubles["IntersectOffAngle"] = offAngle;

                var offVarName = item.StringParams.TryGetValue("IntersectOffsetVar", out var ov) && !string.IsNullOrEmpty(ov) ? ov : "两线交点_Offset";
                var offVar = func.Params.Variables.FirstOrDefault(v => v.Name == offVarName);
                if (offVar == null) { offVar = new LocalVariableItem(offVarName, "Pos", new Pos());
                    System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(offVar)); }
                offVar.RawValue = new Pos(offX, offY, 0, offAngle);
            }

            // 画交点 — 写入 DrawCommands
            func.DrawCommands.Add(DrawCommand.Circle(cross.X, cross.Y, 8, System.Windows.Media.Color.FromArgb(128, 255, 0, 255), 2));
            func.DrawCommands.Add(DrawCommand.FilledCircle(cross.X, cross.Y, 3, System.Windows.Media.Color.FromArgb(128, 255, 0, 255)));

            var resultVarName = item.StringParams.TryGetValue("IntersectResultVar", out var rv) && !string.IsNullOrEmpty(rv) ? rv : "两线交点";
            var resultVar = func.Params.Variables.FirstOrDefault(v => v.Name == resultVarName);
            if (resultVar == null)
            {
                resultVar = new LocalVariableItem(resultVarName, "Pos", new Pos());
                System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(resultVar));
            }
            resultVar.RawValue = new Pos(ix, iy, 0, intersectAngle);

            item.Flow.Done = true;
            return true;
        }
    }
}
