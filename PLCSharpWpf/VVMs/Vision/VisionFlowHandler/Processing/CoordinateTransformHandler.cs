using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// 坐标转换：将图像坐标（ImageX, ImageY）通过指定的变换矩阵转换为世界坐标（WorldX, WorldY）。
    /// 变换矩阵从局部图像列表（Params.Mats）中按名称选取。
    /// 支持 2×3 仿射矩阵和 3×3 透视变换矩阵（CV_64F / CV_32F）。
    /// </summary>
    public class CoordinateTransformHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 当前处理器的功能类型标识
        /// </summary>
        public VisionFlowType Type => VisionFlowType.坐标转换;

        /// <summary>
        /// 执行坐标转换：读取图像坐标，应用变换矩阵，输出世界坐标
        /// </summary>
        /// <param name="func">所属的视觉功能实例</param>
        /// <param name="item">当前流程步骤，包含图像坐标和矩阵名称参数</param>
        /// <returns>转换成功返回 true，参数缺失或矩阵无效返回 false</returns>
        public bool Execute(VisionFunction func, VisionFlow item)
        {
            // 1. 从局部变量表读取变量，根据类型提取图像坐标
            if (!item.StringParams.TryGetValue("InputVar", out var varName) || string.IsNullOrEmpty(varName))
                throw new Exception("输入变量未设置");
            var variable = func.Params.Variables.FirstOrDefault(v => v.Name == varName);
            if (variable == null) throw new Exception("输入变量不存在");

            double imgX = 0, imgY = 0;
            switch (variable.VarType)
            {
                case "Pos": if (variable.RawValue is Pos p) { imgX = p.X; imgY = p.Y; } else return false; break;
                case "Circle": if (variable.RawValue is Circle c) { imgX = c.Center.X; imgY = c.Center.Y; } else return false; break;
                case "Rect": if (variable.RawValue is Rect r) { imgX = r.Center.X; imgY = r.Center.Y; } else return false; break;
                case "Line": if (variable.RawValue is Line l) { imgX = (l.From.X + l.To.X) / 2; imgY = (l.From.Y + l.To.Y) / 2; } else return false; break;
                default: return false;
            }

            // 2. 从全局变量中读取变换矩阵
            if (!item.StringParams.TryGetValue("TransformMat", out string? matName) ||
                string.IsNullOrEmpty(matName))
                item.StringParams["TransformMat"] = "标定矩阵";

            var matVar = func.GlobalModel.VariablesModel.SystemVariables.FirstOrDefault(v => v.Name == matName);
            if (matVar?.Value == null)
                throw new Exception("标定矩阵不存在，请标定");

            Mat transformMat = MatExtension.DeserializeAffineMat(matVar.Value.ToString());

            // 4. 将 Mat 转换为 CV_64F 以便统一读取
            if (transformMat.Type() != MatType.CV_64FC1)
            {
                Mat converted = new();
                transformMat.ConvertTo(converted, MatType.CV_64F);
                transformMat = converted;
            }

            // 5. 根据矩阵尺寸应用变换
            double wx = 0, wy = 0;
            if (transformMat.Rows == 2 && transformMat.Cols == 3)
            {
                // 2×3 仿射变换
                // [a  b  tx]   [x]   [x']
                // [c  d  ty] × [y] = [y']
                //               [1]
                wx = imgX * transformMat.At<double>(0, 0)
                          + imgY * transformMat.At<double>(0, 1)
                          + transformMat.At<double>(0, 2);
                wy = imgX * transformMat.At<double>(1, 0)
                          + imgY * transformMat.At<double>(1, 1)
                          + transformMat.At<double>(1, 2);

                func.Params.ResultDoubles["WorldX"] = wx;
                func.Params.ResultDoubles["WorldY"] = wy;
            }
            else if (transformMat.Rows == 3 && transformMat.Cols == 3)
            {
                // 3×3 透视变换（单应性矩阵）
                // [h00 h01 h02]   [x]   [x']
                // [h10 h11 h12] × [y] = [y']
                // [h20 h21 h22]   [1]   [w ]  需透视除法
                double w = imgX * transformMat.At<double>(2, 0)
                         + imgY * transformMat.At<double>(2, 1)
                         + transformMat.At<double>(2, 2);
                if (Math.Abs(w) < 1e-10)
                    throw new Exception("标定矩阵奇异矩阵，请重新标定"); // 奇异矩阵

                wx = (imgX * transformMat.At<double>(0, 0)
                           + imgY * transformMat.At<double>(0, 1)
                           + transformMat.At<double>(0, 2)) / w;
                wy = (imgX * transformMat.At<double>(1, 0)
                           + imgY * transformMat.At<double>(1, 1)
                           + transformMat.At<double>(1, 2)) / w;

                func.Params.ResultDoubles["WorldX"] = wx;
                func.Params.ResultDoubles["WorldY"] = wy;
            }
            else
            {
                throw new Exception("不支持的矩阵尺寸，请重新标定"); // 不支持的矩阵尺寸
            }

            // 输出到变量
            if (!item.StringParams.TryGetValue("OutputVar", out var outName) || string.IsNullOrEmpty(outName))
            {
                item.StringParams["OutputVar"] = "WorldPos";
            }
            var outVar = func.Params.Variables.FirstOrDefault(v => v.Name == outName);
            if (outVar == null)
            {
                outVar = new LocalVariableItem(outName, "Pos", new Pos());
                System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(outVar));
            }
            outVar.RawValue = new Pos(wx, wy, 0, 0);
            return true;
        }

    }
}
