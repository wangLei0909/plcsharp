using PLCSharp.Core.Prism;
using PLCSharp.Core.Tools;
using PLCSharp.Models;
using PLCSharp.VVMs.Robots.Epson;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace PLCSharp.VVMs.Robots
{
    /// <summary>
    /// 机器人模型
    /// </summary>
    [Model]
    public class RobotModel : ModelBase
    {
        /// <summary>
        /// 机器人模型
        /// </summary>
        public RobotModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += BackgroundWork;
            if (!bkgWorker.IsBusy)
                bkgWorker.RunWorkerAsync();
        }
        private readonly BackgroundWorker bkgWorker;
        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                Thread.Sleep(1000);
                // 机器人状态轮询由各自的后台线程处理
            }
        }


        #region 全局
        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }


        private ObservableCollection<Robot> _Robots = [];
        /// <summary>
        /// Robots
        /// </summary>
        public ObservableCollection<Robot> Robots
        {
            get { return _Robots; }
            set { SetProperty(ref _Robots, value); }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="globalModel">全局模型</param>
        public void Init(GlobalModel globalModel)
        {
            GlobalModel = globalModel;
            foreach (var item in _DatasContext.Robots)
            {
              
                switch (item.Type)
                {
                    case RobotType.Undefined:
                        break;
                    case RobotType.Epson:
                        var epsonRobot = EpsonRobot.Create(item);
                        epsonRobot.Model = this;
                        Robots.Add(epsonRobot);
                        break;
                    default:
                        break;
                }



            }

        }
        #endregion

        public void LoadRecipe(Guid CurrentRecipeID)
        {


            foreach (var robot in Robots)
            {
                robot.Points.Clear();

                foreach (var item in _DatasContext.RobotPoints)
                {
                    if (item.RobotID == robot.ID && item.RecipeID == CurrentRecipeID)
                    {

                        var point = item.DeepCopy();

                        robot.Points.Add(point);
                    }
                }

                robot.RobotMatrices.Clear();
                foreach (var item in _DatasContext.RobotMatrices)
                {


                    if (item.RobotID == robot.ID && item.RecipeID == CurrentRecipeID)
                    {

                        var matrix = item.DeepCopy();
                        robot.Create(matrix);
                        robot.RobotMatrices.Add(matrix);
                    }
                }

            }


        }


    }

    /// <summary>
    /// 表示 4 轴 SCARA 机械手的工具坐标系及坐标转换工具
    /// </summary>
    /// <remarks>
    /// 对于 4 轴 SCARA：
    ///   (X, Y) = 手腕中心在机器人世界坐标系中的位置
    ///   Rz = 0（无额外旋转，SCARA 的 U 轴即绕 Z 旋转）
    /// 当 Rz=0 时，坐标转换自然退化为 SCARA 运动学公式。
    /// </remarks>
    public class ToolFrame4Axis
    {
        /// <summary>
        /// 手腕中心 X 坐标（世界坐标系）
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// 手腕中心 Y 坐标（世界坐标系）
        /// </summary>
        public double Y { get; set; }
        /// <summary>
        /// Z 坐标偏移（世界坐标系）
        /// </summary>
        public double Z { get; set; }
        /// <summary>
        /// 绕 Z 轴的旋转角度（单位：度）。SCARA 工具坐标应设为 0。
        /// </summary>
        public double Rz { get; set; }

        private double RzRadians => Rz * (Math.PI / 180.0);
        /// <summary>
        /// 【工具坐标 → 世界坐标】
        /// 工具坐标 (X, Y) 是工具 TCP 在工具坐标系中的偏移量，
        /// 转换时先由 U 轴旋转再叠加 Rz，最后平移到手腕中心。
        /// </summary>
        public static RobotPoint ToolToWorld(RobotPoint toolPoint, ToolFrame4Axis toolFrame)
        {
            if (toolPoint == null)
                throw new ArgumentNullException(nameof(toolPoint));
            if (toolFrame == null)
                throw new ArgumentNullException(nameof(toolFrame));

            // ① U 轴旋转：工具点 XY 先绕自身 U 角旋转（U=0° 参考偏移 → 实际偏移）
            double uRad = toolPoint.U * (Math.PI / 180.0);
            double cosU = Math.Cos(uRad);
            double sinU = Math.Sin(uRad);
            double xAfterU = toolPoint.X * cosU - toolPoint.Y * sinU;
            double yAfterU = toolPoint.X * sinU + toolPoint.Y * cosU;

            // ② 工具坐标系 Rz 旋转 + ③ 平移
            double cosRz = Math.Cos(toolFrame.RzRadians);
            double sinRz = Math.Sin(toolFrame.RzRadians);

            return new RobotPoint
            {
                X = toolFrame.X + xAfterU * cosRz - yAfterU * sinRz,
                Y = toolFrame.Y + xAfterU * sinRz + yAfterU * cosRz,
                Z = toolPoint.Z,
                U = toolPoint.U + toolFrame.Rz,
                Safe = toolPoint.Safe,
                Command = toolPoint.Command,
                Hand = toolPoint.Hand,
                Rate = toolPoint.Rate,
            };
        }

        /// <summary>
        /// 【世界坐标 → 工具坐标】
        /// 反向还原：先减原点、反旋转 Rz，再反旋转 U 得到 U=0° 参考偏移。
        /// </summary>
        public static RobotPoint WorldToTool(RobotPoint worldPoint, ToolFrame4Axis toolFrame)
        {
            if (worldPoint == null)
                throw new ArgumentNullException(nameof(worldPoint));
            if (toolFrame == null)
                throw new ArgumentNullException(nameof(toolFrame));

            // ③' 减去工具原点偏移 + ②' 反向 Rz 旋转
            double cosRz = Math.Cos(toolFrame.RzRadians);
            double sinRz = Math.Sin(toolFrame.RzRadians);
            double dx = worldPoint.X - toolFrame.X;
            double dy = worldPoint.Y - toolFrame.Y;
            double xAfterRz = dx * cosRz + dy * sinRz;
            double yAfterRz = -dx * sinRz + dy * cosRz;

            // ①' 反向 U 旋转——还原为 U=0° 参考偏移（U_tool = U_world - Rz）
            double uTool = worldPoint.U - toolFrame.Rz;
            double uRad = uTool * (Math.PI / 180.0);
            double cosU = Math.Cos(uRad);
            double sinU = Math.Sin(uRad);
            double xTool = xAfterRz * cosU + yAfterRz * sinU;
            double yTool = -xAfterRz * sinU + yAfterRz * cosU;

            return new RobotPoint
            {
                X = xTool,
                Y = yTool,
                Z = worldPoint.Z,
                U = uTool,
                Safe = worldPoint.Safe,
                Command = worldPoint.Command,
                Hand = worldPoint.Hand,
                Rate = worldPoint.Rate,
            };
        }

        /// <summary>
        /// 根据两个世界坐标点生成 4 轴工具坐标系（SCARA 标定法）
        /// </summary>
        /// <param name="p1">
        /// 第一个标定点：机器人以某个 U 角（U1）将工具 TCP 对准参考点，
        /// 记录此时的机器人世界坐标 (X1, Y1, Z1, U1)
        /// </param>
        /// <param name="p2">
        /// 第二个标定点：保持工具 TCP 不动，旋转 U 角到另一个值（U2），
        /// 记录此时的机器人世界坐标 (X2, Y2, Z2, U2)
        /// </param>
        /// <returns>生成的 4 轴工具坐标系</returns>
        /// <remarks>
        /// 4 轴 SCARA 机器人的 U 轴旋转时，工具 TCP 绕手腕中心画圆弧。
        /// 本方法从两个不同 U 角的 TCP 位置反算出：
        ///   1) 工具偏移量 (Tx, Ty) — U=0° 时 TCP 在手腕坐标系中的偏移
        ///   2) 手腕中心位置 (WCx, WCy)
        /// 两点的 U 角差建议 ≥ 30°（90° 时精度最佳）。
        /// </remarks>
        public static ToolFrame4Axis GenerateFromTwoPoints(RobotPoint p1, RobotPoint p2)
        {
            // 1. 检查 U 角差异，防止 ΔR 奇异
            double u1Rad = p1.U * (Math.PI / 180.0);
            double u2Rad = p2.U * (Math.PI / 180.0);
            double deltaURad = Math.Abs(u1Rad - u2Rad);
            if (deltaURad < 0.087) // < 5°
            {
                throw new ArgumentException("两点的 U 角度差过小，无法计算工具偏移，请确保 U 角差异 ≥ 5°。");
            }

            double cosU1 = Math.Cos(u1Rad);
            double sinU1 = Math.Sin(u1Rad);
            double cosU2 = Math.Cos(u2Rad);
            double sinU2 = Math.Sin(u2Rad);

            // 2. ΔR = R(U1) - R(U2) 的矩阵元素
            //    R(U) = [cosU, -sinU; sinU, cosU]
            double a = cosU1 - cosU2; // = d
            double c = sinU1 - sinU2; // b = -c
            double det = a * a + c * c;

            // 理论上 det = 2 - 2·cos(ΔU)，仅 ΔU=0° 时为 0，此处安全兜底
            if (det < 1e-10)
            {
                throw new ArgumentException("两点的 U 角度差过小，无法计算工具偏移。");
            }

            // 3. 求解工具偏移量 [Tx; Ty]
            //    P1 - P2 = ΔR · [Tx; Ty]
            //    [Tx; Ty] = ΔR⁻¹ · (P1 - P2)
            double dpx = p1.X - p2.X;
            double dpy = p1.Y - p2.Y;

            double invDet = 1.0 / det;
            // ΔR⁻¹ = 1/det · [a, c; -c, a]
            double tx = invDet * (a * dpx + c * dpy);
            double ty = invDet * (-c * dpx + a * dpy);

            // 4. 计算手腕中心位置
            //    P1 = WC + R(U1) · [Tx; Ty]
            //    WC = P1 - R(U1) · [Tx; Ty]
            double wcx = p1.X - (tx * cosU1 - ty * sinU1);
            double wcy = p1.Y - (tx * sinU1 + ty * cosU1);

            // 5. 构建并返回工具坐标系
            //    X, Y = 手腕中心；Rz = 0（SCARA 无额外旋转）
            return new ToolFrame4Axis
            {
                X = wcx,
                Y = wcy,
                Z = p1.Z,
                Rz = 0.0
            };
        }
    }
}
