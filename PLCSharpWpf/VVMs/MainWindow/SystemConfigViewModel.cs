using MiniExcelLibs;
using Newtonsoft.Json;
using PLCSharp.Core.Prism;
using PLCSharp.Models;
using PLCSharp.VVMs.Connects;
using PLCSharp.VVMs.Connects.ModbusRtu;
using PLCSharp.VVMs.Connects.ModbusTcp;
using PLCSharp.VVMs.Connects.SerialPort;
using PLCSharp.VVMs.Connects.Socket;
using PLCSharp.VVMs.MotionController;
using PLCSharp.VVMs.Robots;
using PLCSharp.VVMs.Robots.Epson;
using PLCSharp.VVMs.Vision.Camera;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.IO;

namespace PLCSharp.VVMs.MainWindow
{
    /// <summary>
    /// System配置视图模型
    /// </summary>
    public class SystemConfigViewModel : DialogAwareBase
    {
        /// <summary>
        /// System配置视图模型
        /// </summary>
        public SystemConfigViewModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            GlobalModel = container.Resolve<GlobalModel>();
            Navigate = container.Resolve<NavigateModel>();
            LoadConfig();
        }
        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }
        /// <summary>
        /// Navigate
        /// </summary>
        public NavigateModel Navigate { get; set; }

        /// <summary>
        /// EnvironmentVariables
        /// </summary>
        public ObservableCollection<EnvironmentVariableItem> EnvironmentVariables { get; set; } = [];

        private EnvironmentVariableItem _SelectedEnvironmentVariable;
        /// <summary>
        /// SelectedEnvironment变量
        /// </summary>
        public EnvironmentVariableItem SelectedEnvironmentVariable
        {
            get { return _SelectedEnvironmentVariable; }
            set { SetProperty(ref _SelectedEnvironmentVariable, value); }
        }

        private void LoadConfig()
        {
            if (!File.Exists("./Config/EnvironmentVariable.json")) return;
            try
            {
                var json = File.ReadAllText("./Config/EnvironmentVariable.json");
                var config = JsonConvert.DeserializeAnonymousType(json, new { EnvironmentVariables = new List<EnvironmentVariableItem>() });
                if (config?.EnvironmentVariables != null)
                {
                    EnvironmentVariables = [.. config.EnvironmentVariables];
                    RaisePropertyChanged(nameof(EnvironmentVariables));
                }
            }
            catch (Exception)
            {



            }
        }

        private void SaveConfig()
        {
            if (!Directory.Exists("./Config/"))
            {
                Directory.CreateDirectory("./Config/");
            }

            var config = new { EnvironmentVariables = EnvironmentVariables.ToList() };
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText("./Config/EnvironmentVariable.json", json);

            SendInfoDialog("环境变量已保存");
        }

        private DelegateCommand _AddEnvironmentVariable;
        /// <summary>
        /// 添加Environment变量
        /// </summary>
        public DelegateCommand AddEnvironmentVariable =>
            _AddEnvironmentVariable ??= new DelegateCommand(ExecuteAddEnvironmentVariable);

        void ExecuteAddEnvironmentVariable()
        {
            EnvironmentVariables.Add(new EnvironmentVariableItem());
        }

        private DelegateCommand _DeleteEnvironmentVariable;
        /// <summary>
        /// 删除Environment变量
        /// </summary>
        public DelegateCommand DeleteEnvironmentVariable =>
            _DeleteEnvironmentVariable ??= new DelegateCommand(ExecuteDeleteEnvironmentVariable);

        void ExecuteDeleteEnvironmentVariable()
        {
            if (SelectedEnvironmentVariable != null)
            {
                EnvironmentVariables.Remove(SelectedEnvironmentVariable);
                SelectedEnvironmentVariable = null;
            }
        }

        private DelegateCommand _Save;
        /// <summary>
        /// 保存
        /// </summary>
        public DelegateCommand Save =>
            _Save ??= new DelegateCommand(ExecuteSave);

        void ExecuteSave()
        {
            SaveConfig();
        }

        private DelegateCommand _BrowsePath;
        /// <summary>
        /// Browse路径
        /// </summary>
        public DelegateCommand BrowsePath =>
            _BrowsePath ??= new DelegateCommand(ExecuteBrowsePath);

        void ExecuteBrowsePath()
        {
            if (SelectedEnvironmentVariable == null) return;

            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "选择文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedEnvironmentVariable.Value = dialog.FolderName;
            }
        }

        private DelegateCommand _ExportHardwareConfig;
        /// <summary>
        /// ExportHardware配置
        /// </summary>
        public DelegateCommand ExportHardwareConfig =>
            _ExportHardwareConfig ??= new DelegateCommand(ExecuteExportHardwareConfig);

        void ExecuteExportHardwareConfig()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel文件(*.xlsx)|*.xlsx",
                FileName = $"硬件配置_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Export(dialog.FileName);
                    SendInfoDialog($"硬件配置已导出: {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    SendInfoDialog($"导出失败: {ex.Message}"    );
                }
            }
        }

        private DelegateCommand _ImportHardwareConfig;
        /// <summary>
        /// ImportHardware配置
        /// </summary>
        public DelegateCommand ImportHardwareConfig =>
            _ImportHardwareConfig ??= new DelegateCommand(ExecuteImportHardwareConfig);

        void ExecuteImportHardwareConfig()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel文件(*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Import(dialog.FileName);
                    SendInfoDialog("硬件配置已导入，请保存确认");
                }
                catch (Exception ex)
                {
                    SendInfoDialog($"导入失败: {ex.Message}");
                }
            }
        }


        /// <summary>导出硬件配置到 Excel 文件，每个模块一个 sheet</summary>
        public void Export(string filePath)
        {
            var sheets = new Dictionary<string, object>();

            // Camera
            sheets["Camera"] = GlobalModel.VisionsModel.Cameras
                .Select(c => new
                {
                    ID = c.ID.ToString(),
                    c.Name,
                    Brand = c.Brand.ToString(),
                    c.Comment,
                    c.SerializedParams
                }).ToList();

            // Controller
            sheets["Controller"] = GlobalModel.ControllersModel.Controllers
                .Select(c => new
                {
                    ID = c.ID.ToString(),
                    c.ControllerNo,
                    Type = c.Type.ToString(),
                    c.IP,
                    c.Comment
                }).ToList();

            // Axis
            sheets["Axis"] = GlobalModel.ControllersModel.Axes
                .Select(a => new
                {
                    ID = a.ID.ToString(),
                    a.ControllerNumber,
                    a.Name,
                    a.AxisNo,
                    a.Comment,
                    a.SerializedParams
                }).ToList();

            // Robot
            sheets["Robot"] = GlobalModel.RobotModel.Robots
                .Select(r => new
                {
                    ID = r.ID.ToString(),
                    r.Name,
                    Type = r.Type.ToString(),
                    r.IP,
                    r.Port,
                    r.CommanPort,
                    r.Speed,
                    r.Comment,
                    r.SerializedParams,
                    r.SerializedTools
                }).ToList();

            // Connect
            sheets["Connect"] = GlobalModel.Connects.Connects
                .Select(c => new
                {
                    ID = c.ID.ToString(),
                    c.Name,
                    c.IP_SerialPort,
                    c.Port,
                    Type = c.Type.ToString(),
                    c.Comment,
                    c.SerializedParams,
                    c.SerializedDataItems,
                    c.SerializedClients
                }).ToList();

            MiniExcel.SaveAs(filePath, sheets);
        }

        /// <summary>从 Excel 文件导入硬件配置，按名称匹配更新或新增</summary>
        public void Import(string filePath)
        {
            ImportCameras(MiniExcel.Query(filePath, sheetName: "Camera"));
            ImportControllers(MiniExcel.Query(filePath, sheetName: "Controller"));
            ImportAxes(MiniExcel.Query(filePath, sheetName: "Axis"));
            ImportRobots(MiniExcel.Query(filePath, sheetName: "Robot"));
            ImportConnects(MiniExcel.Query(filePath, sheetName: "Connect"));
        }

        private void ImportCameras(IEnumerable<dynamic> rows)
        {
            foreach (var row in rows)
            {
                var idStr = (string)row.ID;
                if (string.IsNullOrWhiteSpace(idStr)) continue;
                if (!Guid.TryParse(idStr, out var id)) continue;

                // ID已存在则跳过
                if (GlobalModel.VisionsModel.Cameras.Any(c => c.ID == id)) continue;

                var name = (string)row.Name;
                if (string.IsNullOrWhiteSpace(name)) continue;

                // 名称已存在则跳过
                if (GlobalModel.VisionsModel.Cameras.Any(c => c.Name == name)) continue;

                var camera = new CameraBase
                {
                    ID = id,
                    Name = name
                };
                if (Enum.TryParse<CameraBrand>((string)row.Brand, out var brand))
                    camera.Brand = brand;
                camera.Comment = (string)row.Comment;
                camera.SerializedParams = (string)row.SerializedParams;
                GlobalModel.VisionsModel.Cameras.Add(camera);
            }
        }

        private void ImportControllers(IEnumerable<dynamic> rows)
        {
            foreach (var row in rows)
            {
                var idStr = (string)row.ID;
                if (string.IsNullOrWhiteSpace(idStr)) continue;
                if (!Guid.TryParse(idStr, out var id)) continue;

                // ID已存在则跳过
                if (GlobalModel.ControllersModel.Controllers.Any(c => c.ID == id)) continue;

                var controllerNo = (ushort)row.ControllerNo;
                // ControllerNo已存在则跳过
                if (GlobalModel.ControllersModel.Controllers.Any(c => c.ControllerNo == controllerNo)) continue;
                if (!Enum.TryParse<ControllerType>((string)row.Type, out var type)) continue;

                var controller = new Controller
                {
                    ID = id,
                    ControllerNo = controllerNo,
                    Type = type,
                    IP = (string)row.IP,
                    Comment = (string)row.Comment
                };
                GlobalModel.ControllersModel.Controllers.Add(controller);
            }
        }

        private void ImportAxes(IEnumerable<dynamic> rows)
        {
            foreach (var row in rows)
            {
                var idStr = (string)row.ID;
                if (string.IsNullOrWhiteSpace(idStr)) continue;
                if (!Guid.TryParse(idStr, out var id)) continue;

                // ID已存在则跳过
                if (GlobalModel.ControllersModel.Axes.Any(a => a.ID == id)) continue;

                var name = (string)row.Name;
                if (string.IsNullOrWhiteSpace(name)) continue;

                // 名称已存在则跳过
                if (GlobalModel.ControllersModel.Axes.Any(a => a.Name == name)) continue;

                var axis = new Axis
                {
                    ID = id,
                    Name = name,
                    ControllerNumber = (ushort)row.ControllerNumber,
                    AxisNo = (ushort)row.AxisNo,
                    Comment = (string)row.Comment
                };
                axis.SerializedParams = (string)row.SerializedParams;
                GlobalModel.ControllersModel.Axes.Add(axis);
            }
        }

        private void ImportRobots(IEnumerable<dynamic> rows)
        {
            foreach (var row in rows)
            {
                var idStr = (string)row.ID;
                if (string.IsNullOrWhiteSpace(idStr)) continue;
                if (!Guid.TryParse(idStr, out var id)) continue;

                // ID已存在则跳过
                if (GlobalModel.RobotModel.Robots.Any(r => r.ID == id)) continue;

                var name = (string)row.Name;
                if (string.IsNullOrWhiteSpace(name)) continue;

                // 名称已存在则跳过
                if (GlobalModel.RobotModel.Robots.Any(r => r.Name == name)) continue;

                if (!Enum.TryParse<RobotType>((string)row.Type, out var type)) continue;

                Robot robot = type == RobotType.Epson
                    ? EpsonRobot.Create(new Robot { Name = name, Type = type })
                    : new Robot { Name = name, Type = type };
                robot.ID = id;
                robot.IP = (string)row.IP;
                robot.Port = (int)row.Port;
                robot.CommanPort = (int)row.CommanPort;
                robot.Speed = (int)row.Speed;
                robot.Comment = (string)row.Comment;
                robot.SerializedParams = (string)row.SerializedParams;
                robot.SerializedTools = (string)row.SerializedTools;
                GlobalModel.RobotModel.Robots.Add(robot);
            }
        }

        private void ImportConnects(IEnumerable<dynamic> rows)
        {
            foreach (var row in rows)
            {
                var idStr = (string)row.ID;
                if (string.IsNullOrWhiteSpace(idStr)) continue;
                if (!Guid.TryParse(idStr, out var id)) continue;

                // ID已存在则跳过
                if (GlobalModel.Connects.Connects.Any(c => c.ID == id)) continue;

                var name = (string)row.Name;
                if (string.IsNullOrWhiteSpace(name)) continue;

                // 名称已存在则跳过
                if (GlobalModel.Connects.Connects.Any(c => c.Name == name)) continue;

                var typeStr = (string)row.Type;
                if (!Enum.TryParse<ProtocolType>(typeStr, out var protocolType)) continue;

                Connect connect = protocolType switch
                {
                    ProtocolType.SocketClient => new SocketClient(),
                    ProtocolType.SocketSever => new SocketServer(),
                    ProtocolType.ModbusTcpClient => new ModbusTcpClient(),
                    ProtocolType.ModbusTcpServer => new ModbusTcpServer(),
                    ProtocolType.ModbusRtuClient => new ModbusRtuClient(),
                    ProtocolType.FreeSerialProtocol => new FreeSerialProtocol(),
                    _ => null
                };
                if (connect == null) continue;

                connect.ID = id;
                connect.Name = name;
                connect.IP_SerialPort = (string)row.IP_SerialPort;
                connect.Port = (int)row.Port;
                connect.Type = protocolType;
                connect.Comment = (string)row.Comment;
                connect.SerializedParams = (string)row.SerializedParams;
                connect.SerializedDataItems = (string)row.SerializedDataItems;
                connect.SerializedClients = (string)row.SerializedClients;
                GlobalModel.Connects.Connects.Add(connect);
            }
        }
    }
}
