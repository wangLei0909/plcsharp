using PLCSharp.Core.Tools;
using PLCSharp.VVMs.Connects.Socket;
using System.Threading;

namespace PLCSharp.VVMs.Robots.Epson;

/// <summary>
/// 爱普生机器人
/// </summary>
public class EpsonRobot : Robot
{
    /// <summary>
    /// 客户端Ctrl
    /// </summary>
    public SocketClient ClientCtrl { get; set; }

    /// <summary>
    /// 客户端Command
    /// </summary>
    public SocketClient ClientCommand { get; set; }


    /// <summary>
    /// 创建
    /// </summary>
    /// <param name="robot">机器人</param>
    /// <returns>返回结果</returns>
    public static EpsonRobot Create(Robot robot)
    {
        var r = new EpsonRobot
        {
            ID = robot.ID,
            Name = robot.Name,
            Type = robot.Type,
            IP = robot.IP,
            Port = robot.Port,
            CommanPort = robot.CommanPort,
            Tools = robot.Tools,
            ClientCtrl = new SocketClient { IP_SerialPort = robot.IP, Port = robot.Port },
            ClientCommand = new SocketClient { IP_SerialPort = robot.IP, Port = robot.CommanPort }
        };
        r.StatusInfo[0] = "Test"; //Test	在TEST模式下打开
        r.StatusInfo[1] = "Teach"; //Teach	在TEACH模式下打开
        r.StatusInfo[2] = "Auto"; //Auto	在远程输入接受条件下打开
        r.StatusInfo[3] = "Warning"; //Warning   发生警告时打开
        r.StatusInfo[4] = "SError"; //SError	在严重错误状态下打开发生严重错误时，“Reset输入”不起作用。重启控制器进行恢复
        r.StatusInfo[5] = "Safeguar"; //Safeguard	安全门打开时打开
        r.StatusInfo[6] = "EStop"; //EStop	在紧急停止状态下打开
        r.StatusInfo[7] = "Error"; //Error	在错误状态下打开使用“Reset输入”从错误状态中恢复。
        r.StatusInfo[8] = "Paused"; //Paused	存在暂停任务时打开
        r.StatusInfo[9] = "Running"; //Running   执行任务时打开
        r.StatusInfo[10] = "Ready"; //Ready	控制器完成启动且无任务执行时打开
        r.StatusInfo[12] = "超始点"; //
        r.StatusInfo[13] = "命令连接"; //
        r.StatusInfo[14] = "登陆"; // 登陆
        r.StatusInfo[15] = "控制连接"; //
        r.ConnectRobot();
        return r;
    }

    private void ConnectRobot()
    {
        // 启动状态轮询线程
        var statusThread = new Thread(ReceivesRunState) { IsBackground = true };
        statusThread.Start();

        // 启动命令交互线程
        var commandThread = new Thread(ReceivesRemoteState) { IsBackground = true };
        commandThread.Start();
    }

    #region 状态轮询 (Epson协议)

    private void ReceivesRunState()
    {
        while (true)
        {
            Thread.Sleep(1000);
            try
            {
                ClientCtrl.ReceiveInfo = "";
                if (ClientCtrl.Connected)
                {
                    Status[15] = true; // 在线
                    ClientCtrl.SendMsg("$GetStatus\r\n");
                    Thread.Sleep(100);
                    if (string.IsNullOrEmpty(ClientCtrl.ReceiveInfo)) continue;
                    if (ClientCtrl.ReceiveInfo.Contains("#getstatus", StringComparison.CurrentCultureIgnoreCase)
                        && ClientCtrl.ReceiveInfo.Length >= 27)
                    {
                        //#GetStatus,aaaaaaaaaaa,bbbb
                        //Test/Teach/Auto/Warning/SError/Safeguard/EStop/Error/Paused/Running/Ready
                        //bbbb部：错误 / 警告代码
                        Status[0] = ClientCtrl.ReceiveInfo[11] == '1'; //Test	在TEST模式下打开
                        Status[1] = ClientCtrl.ReceiveInfo[12] == '1'; //Teach	在TEACH模式下打开
                        Status[2] = ClientCtrl.ReceiveInfo[13] == '1'; //Auto	在远程输入接受条件下打开
                        Status[3] = ClientCtrl.ReceiveInfo[14] != '1'; //Warning   发生警告时打开
                        Status[4] = ClientCtrl.ReceiveInfo[15] ==
                                    '1'; //SError	在严重错误状态下打开发生严重错误时，“Reset输入”不起作用。重启控制器进行恢复
                        Status[5] = ClientCtrl.ReceiveInfo[16] == '1'; //Safeguard	安全门打开时打开
                        Status[6] = ClientCtrl.ReceiveInfo[17] == '1'; //EStop	在紧急停止状态下打开
                        Status[7] = ClientCtrl.ReceiveInfo[18] == '1'; //Error	在错误状态下打开使用“Reset输入”从错误状态中恢复。
                        Status[8] = ClientCtrl.ReceiveInfo[19] == '1'; //Paused	存在暂停任务时打开
                        Status[9] = ClientCtrl.ReceiveInfo[20] == '1'; //Running   执行任务时打开
                        Status[10] = ClientCtrl.ReceiveInfo[21] == '1'; //Ready	控制器完成启动且无任务执行时打开
                        Status[14] = true; // 登陆
                        ErrorInfo = ClientCtrl.ReceiveInfo.Substring(23, 4);
                    }
                    else
                    {
                        ClientCtrl.SendMsg("$Login\r\n");
                        Status[14] = false; // 登陆
                    }
                }
                else
                {
                    Status[15] = false; // 在线
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Epson状态轮询异常: {ex.Message}");
            }
        }
    }

    #endregion

    #region 生命周期 (Epson协议)

    /// <summary>
    ///     启动机器人 - 启动命令通道
    /// </summary>
    /// <summary>
    /// 启动
    /// </summary>
    public override void Start()
    {
        ClientCtrl.SendMsg("$Start,0\r\n");
    }

    /// <summary>
    ///     停止机器人
    /// </summary>
    public override void Stop()
    {
        ClientCtrl.SendMsg("$Stop\r\n");
    }

    /// <summary>
    /// 回零
    /// </summary>
    public override void Home()
    {
        var point = new RobotPoint
        {
            Command = "MoveToHome"
        };

        SendCommand(point);
    }

    #endregion


    #region 命令交互 (Epson协议)

    private void ReceivesRemoteState()
    {
        while (true)
            try
            {
                Status[13] = false;
                Thread.Sleep(1000);
                if (Status[14] && !ClientCommand.Connected) continue;
                ClientCommand.SendMsg("@ReturnHere,0\r\n");
                Status[13] = true;
                while (ClientCommand.Connected && Status[13])
                {
                    Thread.Sleep(10);
                    try
                    {
                        RunStateAnalytical(ClientCommand.ReceiveInfo);
                    }

                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Epson状态解析异常: {ex.Message}");
                        Thread.Sleep(1000);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Epson状态轮询外异常: {ex.Message}");
                Thread.Sleep(1000);
            }
    }

    private void RunStateAnalytical(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        var parts = msg.Split('\r')[0].Split(',');
        if (parts.Length == 0)
        {
            RcvShowInfo = "";
            return;
        }
        else
        {
            RcvShowInfo = parts[0].ToLower();
            ClientCommand.ReceiveInfo = "";
            switch (RcvShowInfo)
            {
                case "movetohome":

                    Status[12] = true;
                    CurrToolPoint.X = 0;
                    CurrToolPoint.Y = 0;
                    CurrToolPoint.Z = 0;
                    CurrToolPoint.U = 0;
                    CurrToolPoint.ToolNum = 0;
                    break;
                case "x+":
                case "x-":
                case "y+":
                case "y-":
                case "z+":
                case "z-":
                case "u+":
                case "u-":
                case "returnhere":
                case "runpoint":
                    PointDone = true;
                    if (parts.Length >= 7)
                    {
                        _ = double.TryParse(parts[1], out var x);
                        _ = double.TryParse(parts[2], out var y);
                        _ = double.TryParse(parts[3], out var z);
                        _ = double.TryParse(parts[4], out var u);
                        CurrPoint.X = Math.Round(x, 4);
                        CurrPoint.Y = Math.Round(y, 4);
                        CurrPoint.Z = Math.Round(z, 4);
                        CurrPoint.U = Math.Round(u, 4);
                    }
                    if (PointCommand != null && PointCommand.ToolNum > 0)
                    {

                        var tool = Tools.FirstOrDefault(f => f.Key == PointCommand.ToolNum);
                        if (tool.Value != null)
                        {

                            var toolPoint = ToolFrame4Axis.WorldToTool(CurrPoint, tool.Value);

                            CurrToolPoint.X = toolPoint.X;
                            CurrToolPoint.Y = toolPoint.Y;
                            CurrToolPoint.Z = toolPoint.Z;
                            CurrToolPoint.U = toolPoint.U;
                            CurrToolPoint.ToolNum = PointCommand.ToolNum;

                        }

                    }
                    else
                    {
                        CurrToolPoint.X = 0;
                        CurrToolPoint.Y = 0;
                        CurrToolPoint.Z = 0;
                        CurrToolPoint.U = 0;
                        CurrToolPoint.ToolNum = 0;

                    }
                    break;

            }
        }
    }


    private void SendCommand(RobotPoint point)
    {
        if (!ClientCtrl.Connected)
        {
            throw new Exception($"机器人未连接");
        }
        if (!ClientCommand.Connected)
        {
            throw new Exception($"机器人未启动");
        }
        ClientCommand.ReceiveInfo = "";
        if (!point.Safe)
        {
            throw new Exception($"机器人点位{point.Name}不安全");
        }

        Status[12] = false;


        var speedPercent = (int)(point.Rate * Speed / 100);
        var sendMsg =
            $"@{point.Command} {speedPercent} {point.X} {point.Y} {point.Z} {point.U} {point.V} {point.W},{point.Hand}\r\n";
        ClientCommand.SendMsg(sendMsg);

    }

    /// <summary>
    /// 运行点位
    /// </summary>
    /// <param name="point">点位</param>
    /// <returns>返回布尔值</returns>
    public override void RunPoint(string pointName)
    {
        PointDone = false;
        var point = Points.FirstOrDefault(p => p.Name == pointName) ?? throw new Exception($"机器人点位{pointName}不存在");
        point.Command = "RunPoint";
        PointCommand = point.DeepCopy();
        SendCommand(PointCommand);
    }

    /// <summary>
    ///     点动
    /// </summary>
    /// <param name="pointBase"></param>
    /// <param name="cmd"></param>
    /// <param name="dist"></param>
    /// <param name="rate"></param>
    /// <returns></returns>
    public override void Jog(string pointName, string cmd, double dist, double rate = 0)
    {
        PointDone = false;
 
        var point = Points.FirstOrDefault(p => p.Name == pointName) ?? throw new Exception($"机器人点位{pointName}不存在");

        PointCommand = CurrPoint.DeepCopy();

        if (point.ToolNum == 0)
        {
            switch (cmd)
            {
                case "X+":
                case "X-":
                    PointCommand.Command = cmd;
                    PointCommand.X = dist;
                    PointCommand.Rate = rate > 0 ? rate : point.Rate;
                    SendCommand(PointCommand);
                    break;
                case "Y+":
                case "Y-":
                    PointCommand.Command = cmd;
                    PointCommand.Y = dist;
                    PointCommand.Rate = rate > 0 ? rate : point.Rate;
                    SendCommand(PointCommand);
                    break;
                case "Z+":
                case "Z-":
                    PointCommand.Command = cmd;
                    PointCommand.Z = dist;
                    PointCommand.Rate = rate > 0 ? rate : point.Rate;
                    SendCommand(PointCommand);
                    break;
                case "U+":
                case "U-":
                    PointCommand.Command = cmd;
                    PointCommand.U = dist;
                    PointCommand.Rate = rate > 0 ? rate : point.Rate;
                    SendCommand(PointCommand);
                    break;
            }
        }
        else
        {
            PointCommand.Rate = rate > 0 ? rate : point.Rate;
            PointCommand.Command = "RunPoint";

            var tool = Tools.FirstOrDefault(w => w.Key == point.ToolNum);

            if (tool.Value == null) throw new Exception("工具坐标配置");
            var pointTool = ToolFrame4Axis.WorldToTool(PointCommand, tool.Value);

            switch (cmd)
            {
                case "X+":
                    pointTool.X += dist;
                    break;
                case "X-":
                    pointTool.X -= dist;
                    break;
                case "Y+":
                    pointTool.Y += dist;
                    break;
                case "Y-":
                    pointTool.Y -= dist;
                    break;
                case "Z+":
                    pointTool.Z += dist;
                    break;
                case "Z-":
                    pointTool.Z -= dist;
                    break;
                case "U+":
                    pointTool.U += dist;
                    break;
                case "U-":
                    pointTool.U -= dist;
                    break;


            }
            var pointWord = ToolFrame4Axis.ToolToWorld(pointTool, tool.Value);
            SendCommand(pointWord);
        }

    }

    /// <summary>
    /// Power
    /// </summary>
    /// <param name="cmd">命令参数</param>
    public override void Power(string cmd)
    {
        switch (cmd)
        {
            case "On":
                ClientCtrl.SendMsg("$SetMotorsOn,0\r\n");
                break;
            case "Off":
                ClientCtrl.SendMsg("$SetMotorsOff,0\r\n");
                break;
        }
    }

    #endregion
}