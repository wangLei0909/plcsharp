using DryIoc;
using PLCSharp.Core.Prism;
using PLCSharp.Models;
using PLCSharp.VVMs.Connects.ModbusTcp;
using PLCSharp.VVMs.Connects.Socket;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace PLCSharp.VVMs.Connects
{
    /// <summary>
    /// Connects模型
    /// </summary>
    [Model]
    public class ConnectsModel : ModelBase
    {
        private static DateTime _lastBatchErrorLog = DateTime.MinValue;

        /// <summary>
        /// Connects模型
        /// </summary>
        public ConnectsModel(IContainerExtension container, IEventAggregator ea, IDialogService dialogService) : base(container, ea, dialogService)
        {
            _pollWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            _pollWorker.DoWork += PollWorker_DoWork;
            if (!_pollWorker.IsBusy)
                _pollWorker.RunWorkerAsync();

            foreach (var item in _DatasContext.Connects)
            {
                switch (item.Type)
                {
                    case ProtocolType.SocketClient:
                        {
                            var connect = new SocketClient
                            {
                                ID = item.ID,
                                Name = item.Name,
                                IP_SerialPort = item.IP_SerialPort,
                                Port = item.Port,
                                Type = item.Type,
                                Comment = item.Comment,
                                Params = item.Params,

                            };
                            connect.Params.Prompt = "";
                            Connects.Add(connect);
                        }
                        break;
                    case ProtocolType.SocketSever:
                        {
                            var connect = new SocketServer
                            {
                                ID = item.ID,
                                Name = item.Name,
                                IP_SerialPort = item.IP_SerialPort,
                                Port = item.Port,
                                Type = item.Type,
                                Comment = item.Comment,
                                Clients = item.Clients,
                                Params = item.Params,
                            };
                            connect.Params.Prompt = "";
                            connect.Clients.ToList().ForEach(c => c.Params.Prompt = "");
                            Connects.Add(connect);

                        }
                        break;
                    case ProtocolType.ModbusTcpClient:
                        {
                            var connect = new ModbusTcp.ModbusTcpClient
                            {
                                ID = item.ID,
                                Name = item.Name,
                                IP_SerialPort = item.IP_SerialPort,
                                Port = item.Port,
                                Type = item.Type,
                                Comment = item.Comment,
                                Params = item.Params,
                                DataItems = item.DataItems,
                            };
                            connect.Params.Prompt = "";
                            Connects.Add(connect);
                        }
                        break;
                    case ProtocolType.ModbusTcpServer:
                        {
                            var connect = new ModbusTcp.ModbusTcpServer
                            {
                                ID = item.ID,
                                Name = item.Name,
                                IP_SerialPort = item.IP_SerialPort,
                                Port = item.Port,
                                Type = item.Type,
                                Comment = item.Comment,
                                Clients = item.Clients,
                                Params = item.Params,
                                DataItems = item.DataItems,
                            };
                            connect.Params.Prompt = "";

                            Connects.Add(connect);
                            // 加载时将 DataItems 同步到服务端内存数据表
                            ApplyAllDataToServer(connect);
                        }
                        break;
                    case ProtocolType.ModbusRtuClient:
                        {
                            var connect = new ModbusRtu.ModbusRtuClient
                            {
                                ID = item.ID,
                                Name = item.Name,
                                IP_SerialPort = item.IP_SerialPort,
                                Port = item.Port,
                                Type = item.Type,
                                Comment = item.Comment,
                                Params = item.Params,
                                DataItems = item.DataItems,
                            };
                            connect.Params.Prompt = "";
                            Connects.Add(connect);
                        }
                        break;
                    case ProtocolType.FreeSerialProtocol:
                        {
                            var connect = new SerialPort.FreeSerialProtocol
                            {
                                ID = item.ID,
                                Name = item.Name,
                                IP_SerialPort = item.IP_SerialPort,
                                Port = item.Port,
                                Type = item.Type,
                                Comment = item.Comment,
                                Params = item.Params,
                                DataItems = item.DataItems,
                            };
                            connect.Params.Prompt = "";
                            Connects.Add(connect);
                        }
                        break;
                    default:
                        SendErr("未配置的协议");
                        break;
                }

            }


        }
        #region Connect
        /// <summary>
        /// 全局模型
        /// </summary>
        public GlobalModel GlobalModel { get; set; }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="globalModel">全局模型</param>
        public void Init(GlobalModel globalModel)
        {
            GlobalModel = globalModel;
        }
        private ObservableCollection<Connect> _Connects = [];

        /// <summary>
        /// 连接模型
        /// </summary>
        public ObservableCollection<Connect> Connects
        {
            get { return _Connects; }
            set { SetProperty(ref _Connects, value); }
        }

        private Connect _SelectedConnect;

        /// <summary>
        /// Selected连接
        /// </summary>
        public Connect SelectedConnect
        {
            get { return _SelectedConnect; }
            set { SetProperty(ref _SelectedConnect, value); }
        }

        private DelegateCommand<string> _ConnectsManage;

        /// <summary>
        /// Connects管理
        /// </summary>
        public DelegateCommand<string> ConnectsManage =>
            _ConnectsManage ??= new DelegateCommand<string>(ExecuteConnectsManage);

        private void ExecuteConnectsManage(string cmd)
        {
            switch (cmd)
            {
                case "New":
                    _dialogService.ShowDialog("NewConnect", r =>
                    {
                        if (r.Result == ButtonResult.Yes)
                        {

                            var type = r.Parameters.GetValue<ProtocolType>("Type");

                            switch (type)
                            {
                                case ProtocolType.SocketClient:
                                    Connects.Add(new SocketClient()
                                    {
                                        Name = r.Parameters.GetValue<string>("Name"),
                                        IP_SerialPort = r.Parameters.GetValue<string>("IP_SerialPort"),
                                        Type = type

                                    });
                                    break;
                                case ProtocolType.SocketSever:
                                    Connects.Add(new SocketServer()
                                    {
                                        Name = r.Parameters.GetValue<string>("Name"),
                                        IP_SerialPort = r.Parameters.GetValue<string>("IP_SerialPort"),
                                        Type = type
                                    });
                                    break;
                                case ProtocolType.ModbusTcpClient:
                                    Connects.Add(new ModbusTcp.ModbusTcpClient()
                                    {
                                        Name = r.Parameters.GetValue<string>("Name"),
                                        IP_SerialPort = r.Parameters.GetValue<string>("IP_SerialPort"),
                                        Type = type
                                    });
                                    break;
                                case ProtocolType.ModbusTcpServer:
                                    Connects.Add(new ModbusTcp.ModbusTcpServer()
                                    {
                                        Name = r.Parameters.GetValue<string>("Name"),
                                        IP_SerialPort = r.Parameters.GetValue<string>("IP_SerialPort"),
                                        Type = type
                                    });
                                    break;
                                case ProtocolType.FreeSerialProtocol:
                                    Connects.Add(new SerialPort.FreeSerialProtocol()
                                    {
                                        Name = r.Parameters.GetValue<string>("Name"),
                                        IP_SerialPort = r.Parameters.GetValue<string>("IP_SerialPort"),
                                        Type = type
                                    });
                                    break;
                                case ProtocolType.ModbusRtuClient:
                                    Connects.Add(new ModbusRtu.ModbusRtuClient()
                                    {
                                        Name = r.Parameters.GetValue<string>("Name"),
                                        IP_SerialPort = r.Parameters.GetValue<string>("IP_SerialPort"),
                                        Type = type
                                    });
                                    break;
                                default:
                                    break;
                            }


                        }
                    });
                    break;

                case "Save":
                    SaveConnects();
                    break;

                case "Config":
                    if (SelectedConnect == null) return;
                    switch (SelectedConnect.Type)
                    {
                        case ProtocolType.SocketClient:
                            {
                                var client = Connects.Where(c => c.ID == SelectedConnect.ID).FirstOrDefault();

                                if (client == null)
                                {
                                    client = _container.Resolve<SocketClient>();
                                    {
                                        client.Name = SelectedConnect.Name;
                                        client.IP_SerialPort = SelectedConnect.IP_SerialPort;
                                        client.Port = SelectedConnect.Port;
                                    }
                                    ;
                                    Connects.Add(client);
                                }


                                _dialogService.Show("SocketClientConfig", new DialogParameters($"Name={SelectedConnect.Name}"), r =>
                                {


                                });
                            }
                            break;
                        case ProtocolType.SocketSever:
                            {
                                var server = Connects.Where(c => c.ID == SelectedConnect.ID).FirstOrDefault();

                                if (server == null)
                                {
                                    server = _container.Resolve<SocketServer>();
                                    {
                                        server.Name = SelectedConnect.Name;
                                        server.IP_SerialPort = SelectedConnect.IP_SerialPort;
                                        server.Port = SelectedConnect.Port;
                                    }
                                    ;
                                    Connects.Add(server);
                                }


                                _dialogService.Show("SocketServerConfig", new DialogParameters($"Name={SelectedConnect.Name}"), r =>
                                {


                                });
                            }
                            break;
                        case ProtocolType.ModbusTcpClient:
                            {
                                var client = Connects.Where(c => c.ID == SelectedConnect.ID).FirstOrDefault();

                                if (client == null)
                                {
                                    client = _container.Resolve<ModbusTcp.ModbusTcpClient>();
                                    {
                                        client.Name = SelectedConnect.Name;
                                        client.IP_SerialPort = SelectedConnect.IP_SerialPort;
                                        client.Port = SelectedConnect.Port;
                                    }
                                    ;
                                    Connects.Add(client);
                                }

                                _dialogService.Show("ModbusClientConfig", new DialogParameters($"Name={SelectedConnect.Name}"), r =>
                                {

                                });
                            }
                            break;
                        case ProtocolType.ModbusTcpServer:
                            {
                                var server = Connects.Where(c => c.ID == SelectedConnect.ID).FirstOrDefault();

                                if (server == null)
                                {
                                    server = _container.Resolve<ModbusTcp.ModbusTcpServer>();
                                    {
                                        server.Name = SelectedConnect.Name;
                                        server.IP_SerialPort = SelectedConnect.IP_SerialPort;
                                        server.Port = SelectedConnect.Port;
                                    }
                                    ;
                                    Connects.Add(server);
                                }

                                _dialogService.Show("ModbusServerConfig", new DialogParameters($"Name={SelectedConnect.Name}"), r =>
                                {

                                });
                            }
                            break;
                        case ProtocolType.ModbusRtuClient:
                            {
                                var client = Connects.Where(c => c.ID == SelectedConnect.ID).FirstOrDefault();
                                if (client == null)
                                {
                                    client = _container.Resolve<ModbusRtu.ModbusRtuClient>();
                                    client.Name = SelectedConnect.Name;
                                    client.IP_SerialPort = SelectedConnect.IP_SerialPort;
                                    client.Port = SelectedConnect.Port;
                                    Connects.Add(client);
                                }
                                _dialogService.Show("ModbusRtuConfig", new DialogParameters($"Name={SelectedConnect.Name}"), r => { });
                            }
                            break;
                        case ProtocolType.FreeSerialProtocol:
                            {
                                var client = Connects.Where(c => c.ID == SelectedConnect.ID).FirstOrDefault();
                                if (client == null)
                                {
                                    client = _container.Resolve<SerialPort.FreeSerialProtocol>();
                                    client.Name = SelectedConnect.Name;
                                    client.IP_SerialPort = SelectedConnect.IP_SerialPort;
                                    client.Port = SelectedConnect.Port;
                                    Connects.Add(client);
                                }
                                _dialogService.Show("FreeSerialProtocolConfig", new DialogParameters($"Name={SelectedConnect.Name}"), r => { });
                            }
                            break;
                        default:
                            break;
                    }
                    break;

                case "Remove":
                    if (SelectedConnect != null)
                    {
                        if (System.Windows.MessageBox.Show($"确认删除连接 [{SelectedConnect.Name}]？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            break;
                        if (_DatasContext.Connects.Any(h => h.ID == SelectedConnect.ID))
                        {
                            var removeConnect = _DatasContext.Connects.Where(h => h.ID == SelectedConnect.ID).FirstOrDefault();

                            _DatasContext.Connects.Remove(removeConnect);
                            var name = SelectedConnect.Name;
                            SelectedConnect.Close();
                            Connects.Remove(SelectedConnect);
                            SendInfoDialog($"已删除：{name}");
                            _DatasContext.Save();
                        }


                    }
                    break;
            }
        }

        /// <summary>
        /// 保存Connects
        /// </summary>
        public void SaveConnects()
        {
            var names = new List<string>();

            foreach (var item in Connects)
            {
                if (string.IsNullOrEmpty(item.Name))
                {
                    SendInfoDialog($"保存失败，名称{item.Name}不合适！" );
                    return;
                }

                if (names.Contains(item.Name))
                {
                    SendInfoDialog($"保存失败，重复的名称{item.Name}！");
                    return;
                }
                else
                {
                    names.Add(item.Name);
                }
            }


            foreach (var item in Connects)
            {
                if (!_DatasContext.Connects.Any(h => h.ID == item.ID))
                {
                    _DatasContext.Connects.Add(item);
                }
                else
                {
                    var connect = _DatasContext.Connects.Where(c => c.ID == item.ID).FirstOrDefault();
                    connect.Name = item.Name;
                    connect.IP_SerialPort = item.IP_SerialPort;
                    connect.Comment = item.Comment;
                    connect.Port = item.Port;
                    connect.Params = item.Params;
                    connect.Clients = item.Clients;
                    connect.DataItems = item.DataItems;
                }

            }


            SelectedConnect.Params.Prompt = "";
            _DatasContext.Save();
        }

        #endregion ProtocolHost

        #region ModbusTcpClient 轮询

        private readonly BackgroundWorker _pollWorker;

        private void PollWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                Thread.Sleep(100);

                foreach (var connect in Connects.ToList())
                {
                    if (connect is ModbusTcp.ModbusTcpClient tcpClient && tcpClient.Connected && tcpClient.PollEnabled)
                        PollClient(tcpClient, tcpClient.DataItems);
                    else if (connect is ModbusRtu.ModbusRtuClient rtuClient && rtuClient.Connected && rtuClient.PollEnabled)
                        PollClient(rtuClient, rtuClient.DataItems);
                }
            }
        }

        private void PollClient(Connect client, ObservableCollection<DataItem> items)
        {
            var batches = BuildBatches(items.ToList());
            foreach (var batch in batches)
            {
                if (batch.Items.Count == 0 || !client.Connected) continue;
                ReadBatch(client, batch);
                Thread.Sleep(client.PollIntervalMs);
            }
        }

        private class PollBatch
        {
            /// <summary>
            /// UnitId
            /// </summary>
            public byte UnitId { get; set; }
            /// <summary>
            /// 启动Address
            /// </summary>
            public ushort StartAddress { get; set; }
            /// <summary>
            /// 读取数量
            /// </summary>
            public ushort ReadCount { get; set; }
            /// <summary>
            /// Items
            /// </summary>
            public List<DataItem> Items { get; set; } = [];
        }

        private static List<PollBatch> BuildBatches(List<DataItem> items)
        {
            var batches = new List<PollBatch>();

            var groups = items.GroupBy(x => x.UnitId);
            foreach (var groupitem in groups)
            {
                var sorted = groupitem.OrderBy(i => i.Address).ToList();
                if (sorted.Count == 0) return batches;

                ushort maxRead = 125;
                int idx = 0;
                while (idx < sorted.Count)
                {
                    var batch = new PollBatch { StartAddress = sorted[idx].Address };
                    ushort currentEnd = (ushort)(sorted[idx].Address + GetItemRegisterCount(sorted[idx]));

                    while (idx < sorted.Count)
                    {
                        var item = sorted[idx];
                        ushort itemSize = GetItemRegisterCount(item);
                        ushort itemEnd = (ushort)(item.Address + itemSize);

                        ushort span = (ushort)(itemEnd - batch.StartAddress);
                        if (span > maxRead) break;
                        batch.UnitId = item.UnitId;
                        batch.Items.Add(item);
                        batch.ReadCount = span;
                        if (itemEnd > currentEnd) currentEnd = itemEnd;
                        idx++;
                    }
                    batches.Add(batch);
                }
            }

            return batches;
        }

        private static ushort GetItemRegisterCount(DataItem item)
        {
            return item.ValueInterpretation switch
            {
                DataItem.ValueInterpretationEnum.Int32 or
                DataItem.ValueInterpretationEnum.Float or
                DataItem.ValueInterpretationEnum.String => 2,
                _ => 1,
            };
        }

        private void ReadBatch(Connect client, PollBatch batch)
        {
            try
            {
                ushort[] raw = null;
                if (client is ModbusTcp.ModbusTcpClient tcp)
                    raw = tcp.ReadHoldingRegisters(batch.StartAddress, batch.ReadCount);
                else if (client is ModbusRtu.ModbusRtuClient rtu)
                    raw = rtu.ReadHoldingRegisters(batch.StartAddress, batch.ReadCount, batch.UnitId);

                if (raw == null) return;

                foreach (var item in batch.Items)
                {
                    ushort offset = (ushort)(item.Address - batch.StartAddress);
                    if (offset < raw.Length)
                    {
                        ushort lo = raw[offset];
                        ushort hi = (offset + 1 < raw.Length) ? raw[offset + 1] : (ushort)0;
                        DispatchSetRawValues(item, lo, hi);
                    }
                }
            }
            catch (Exception ex) { if ((DateTime.Now - _lastBatchErrorLog).TotalSeconds > 10) { _lastBatchErrorLog = DateTime.Now; SendErr($"批量读取失败: {ex.Message}"); } }
        }

        private static void DispatchSetRawValues(DataItem item, ushort lo, ushort hi)
        {

            item.SetRawValues(lo, hi);
        }

        #endregion

        /// <summary>写入单个 DataItem 的值到服务器</summary>
        /// <summary>
        /// Apply数据项To服务端
        /// </summary>
        /// <param name="connectName">连接名称</param>
        /// <param name="item">变量项</param>
        public void ApplyDataItemToServer(string connectName, DataItem item)
        {
            var connect = Connects.FirstOrDefault(c => c.Name == connectName);
            ModbusTcp.ModbusTcpClient tcpClient = connect as ModbusTcp.ModbusTcpClient;
            ModbusRtu.ModbusRtuClient rtuClient = connect as ModbusRtu.ModbusRtuClient;
            if (tcpClient == null && rtuClient == null) return;

            try
            {
                item.ParseSendValue();
                bool ok = false;
                ushort[] values = item.ValueInterpretation == DataItem.ValueInterpretationEnum.Int32 ||
                                  item.ValueInterpretation == DataItem.ValueInterpretationEnum.Float ||
                                  item.ValueInterpretation == DataItem.ValueInterpretationEnum.String
                    ? [item.Value, item.HighValue]
                    : [item.Value];

                if (tcpClient != null)
                    ok = values.Length == 1
                        ? tcpClient.WriteSingleRegister(item.Address, values[0])
                        : tcpClient.WriteMultipleRegisters(item.Address, values);
                else if (rtuClient != null)
                    ok = values.Length == 1
                        ? rtuClient.WriteSingleRegister(item.Address, values[0])
                        : rtuClient.WriteMultipleRegisters(item.Address, values);

                if (!ok) connect.Log($"写入失败: HR@{item.Address}");
            }
            catch (Exception ex)
            {
                connect.Log($"写入失败 [HR @ {item.Address}]: {ex.Message}");
            }
        }

        /// <summary>写入连接的所有 DataItem 到服务器</summary>
        public void ApplyAllDataToServer(string connectName)
        {
            var connect = Connects.FirstOrDefault(c => c.Name == connectName);
            if (connect == null) return;

            foreach (var item in connect.DataItems.ToList())
            {
                ApplyDataItemToServer(connectName, item);
            }
        }

        #region ModbusTcpServer 写入（本地内存）

        /// <summary>将单个 DataItem 写入 ModbusTcpServer 内存数据表</summary>
        /// <summary>
        /// Apply数据项To服务端
        /// </summary>
        /// <param name="server">服务端</param>
        /// <param name="item">变量项</param>
        public void ApplyDataItemToServer(ModbusTcp.ModbusTcpServer server, DataItem item)
        {
            item.ParseInputValue();
            WriteRegisterValue(server, item);
        }

        /// <summary>写入连接的所有 DataItem 到 ModbusTcpServer 内存</summary>
        public void ApplyAllDataToServer(ModbusTcp.ModbusTcpServer server)
        {
            foreach (var item in server.DataItems.ToList())
            {
                ApplyDataItemToServer(server, item);
            }
        }

        private static void WriteRegisterValue(ModbusTcp.ModbusTcpServer server, DataItem item)
        {
            if (item.ValueInterpretation == DataItem.ValueInterpretationEnum.Int32 ||
                item.ValueInterpretation == DataItem.ValueInterpretationEnum.Float ||
                item.ValueInterpretation == DataItem.ValueInterpretationEnum.String)
            {
                server.SetHoldingRegister(item.Address, item.Value);
                server.SetHoldingRegister((ushort)(item.Address + 1), item.HighValue);
            }
            else
            {
                server.SetHoldingRegister(item.Address, item.Value);
            }
        }

        private static void SetReg(ModbusTcp.ModbusTcpServer server, ushort addr, ushort val, bool isHolding)
        {
            if (isHolding) server.SetHoldingRegister(addr, val);
            else server.SetInputRegister(addr, val);
        }

        #endregion
    }
}