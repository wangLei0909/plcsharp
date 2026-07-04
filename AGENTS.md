# PLCSharp — PLC 风格上位机控制系统

WPF/.NET 8 上位机软件，通过自由配置硬件、全局变量、界面和流程完成项目开发。

---

## Project

- **技术栈**: .NET 8 (net8.0-windows), WPF, Prism.DryIoc, OpenCvSharp4, EF Core SQLite, Newtonsoft.Json, Natasha (动态编译)
- **入口点**: `PLCSharpWpf/App.xaml.cs` — Prism `Application` 子类，负责 DI 注册、导航配置、全局异常捕获
- **Shell**: `PLCSharpWpf/VVMs/MainWindow/MainWindow.xaml` — 主窗口，包含侧导航 + 内容区
- **语言**: C# (可空引用类型 `#nullable enable`), XAML

## Commands

```
# 编译
dotnet build PLCSharpWpf/PLCSharp.csproj

# 编译（覆盖输出目录，避免进程锁定）
dotnet build PLCSharpWpf/PLCSharp.csproj -p:OutputPath=bin\tmp

# 运行
dotnet run --project PLCSharpWpf/PLCSharp.csproj

# 发布为单文件
dotnet publish PLCSharpWpf/PLCSharp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 清除临时目录
rm -rf PLCSharpWpf/bin/tmp_*
```

> 注意：无单元测试项目，目前无测试命令。

## Architecture

### 分层结构

```
Core/                     # 基础设施层（无业务依赖）
  Prism/                   # 框架扩展：ViewModelBase, DialogAwareBase, CoreAttribute, MessageEvent
  Common/                  # 通用数据模型：FlowModel, CellState, TitleState
  Tools/                   # 工具类：BitTool, FileTools, WpfTool, XPoint, LinqExtension
  UserControls/            # 自定义控件：ImageEdit, RangeSlider, SimpleCell, TreeViewEx
    ROI/                   # ROI 控件：RectROI, RotateRectROI + DiagramDesigner (MoveThumb, ResizeThumb, RotateThumb)
  Resources/Iconfont/      # 图标字体 (iconfont)

Models/                   # 全局数据模型
  GlobalModel.cs           # 全局变量/全局图像/配方/报警等运行时状态（[Model] 单例）
  DatasContext.cs          # EF Core SQLite 数据库上下文
  NavigateModel.cs         # 导航状态（页面列表 + 对话框列表）

VVMs/                     # 业务模块（View-ViewModel 对，按功能分包）
  Authority/               # 用户认证、登录、权限管理
  MainWindow/              # 主窗口壳（标题栏、侧边导航）
  Homepage/                # 主页画布（自定义控件布局 + 表格控件）
  Connects/                # 通讯管理（Modbus TCP/RTU, Socket, SerialPort, 协议类型）
  MotionController/        # 运动控制（EMC/SMC 系列板卡，轴配置，点位管理）
  Vision/                  # 机器视觉（相机配置 + 视觉流程引擎 + 参数编辑）
    Camera/                # 相机封装（海康 HIK 基类 + 配置）
    VisionFlowHandler/     # 视觉流程步骤处理器（策略模式）
      Access/              # 存取图片：相机拍照、文件、全局/局部图像
      Processing/          # 图像处理：灰度化/BGR、阈值、通道拆分、坐标转换
      Algorithm/           # 视觉算法：卡尺寻边、卡尺找圆、卡尺找旋转矩形、ORB 匹配、两线交点
    VisionConfigViewModel.cs  # 视觉主 ViewModel（~66KB，汇总管道调度 + 绘制）
  Workflows/               # 工作流引擎（流程步骤编排、条件分支、动态编译执行）
  ModeState/               # 模式状态机（运行模式切换、状态值管理）
  GlobalVariables/         # 全局变量管理（变量列表、编辑器、导入导出）
  Recipe/                  # 配方管理
  Robots/                  # 机器人配置
  Projects/                # 项目管理

SDK/                      # 第三方 SDK 包装
  Camera/MvCameraControl.Net   # 海康相机 SDK
  MotionControl/EMC       # 固高科技 EMC 运动控制板卡
  MotionControl/SMC       # 固高科技 SMC 运动控制板卡
```

### 关键设计模式

| 模式 | 位置 | 说明 |
|------|------|------|
| **Prism MVVM** | 所有 VVMs 子包 | View-ViewModel 通过 `ViewModelLocationProvider` 依据 `{ViewName}ViewModel` 命名约定自动绑定 |
| **DI 单例注册** | `App.RegisterTypes` | 标有 `[Model]` 特性的类自动注册为单例；标有 `[NavigationPage]` 的 View 自动注册到导航 |
| **策略模式** | `VisionFlowHandler/` | `IVisionFlowHandler` 接口 + 每个 `VisionFlowType` 对应一个 Handler，通过 `VisionFunction` 调度执行 |
| **事件聚合** | `Prism/MessgeEvent.cs` | 通过 `IEventAggregator` 发布订阅错误/日志/消息事件 |
| **对话框** | `DialogAwareBase` | 弹出窗口基类，标有 `[Dialog]` 或 `[DialogMenu]` 的 View 自动注册 |
| **状态机** | `ModeState/` | 运行模式状态管理 |

### 视觉流程引擎 (Vision)

```
VisionFunction (一个"视觉功能"，含多个 VisionFlow 步骤)
  └── VisionFlow[] (步骤列表，每个步骤含 Type + 参数集合)
        └── IVisionFlowHandler.Execute(func, flow) (策略调用)
```

- `VisionConfigViewModel` 持有 `VisionFunction` 列表，管理加载/编辑/执行/绘制
- 执行时遍历 `VisionFunction.VisionFlows`，根据 `VisionFlow.Type` 查找对应的 Handler 并调用 `Execute()`
- 执行结果写入 `func.Src` (Mat 图像)、输出参数写入 `GlobalModel`，绘制指令通过 `DrawCommand` 列表返回

### 绘制系统 (ImageEdit)

`ImageEdit`（`Core/UserControls/ImageEdit.xaml.cs`）是统一的图像绘制控件：
- 支持直线、圆、旋转矩形、矩形的 ROI 绘制和拖拽调整
- 通过 `DrawCommand` 指令列表实现结果可视化覆盖渲染
- 鼠标交互：左键绘制 → 左键进入调整 → 右键结束确认

## Conventions

### 命名

| 规则 | 示例 |
|------|------|
| 解决方案/项目/命名空间 | `PLCSharp`、`PLCSharp.VVMs.Vision` |
| View → ViewModel | `MainWindow.xaml` → `MainWindowViewModel`（自动绑定） |
| Handler 与 VisionFlowType 对应 | `CaliperFindEdgeHandler.Type → VisionFlowType.卡尺寻边` |
| 类文件：大驼峰 | `CaliperFindCircleHandler.cs` |
| 私有字段：下划线前缀 | `_pendingLine`, `_circleAdjustMode` |
| 局部变量/方法参数：小驼峰 | `sx, sy, edgePoints` |

### 编码风格

- **可空引用类型**: 文件头部 `#nullable enable`（GlobalUsings 中已导入基础命名空间）
- **集合初始化**: 使用 `[]`（如 `List<Point2d> edgePoints = [];`）
- **使用集合表达式**: 优先 `[]` 而非 `new List<T>()`
- **Prism 属性**: 结合 `SetProperty(ref _field, value)`
- **EF Core**: `[Key]` 标注主键，`[NotMapped]` 标注非数据库属性
- **JsonIgnore**: 运行时非持久化属性加 `[JsonIgnore]`
- **异步方法**: 使用 `async Task`，方法名 `Async` 后缀（App.xaml.cs 中 `#pragma warning disable VSTHRD200` 豁免了少数平台调用）
- **错误处理**: Handler 内 `throw Exception` 由顶层 `VisionConfigViewModel` 统一捕获显示
- **参数传递**: `VisionFlow` 通过 `StringParams/DoubleParams/IntParams/BoolParams` 字典传递配置

### 视觉模块规范

- 新增视觉工具 = 新增 `VisionFlowType` 枚举 + 实现 `IVisionFlowHandler` + 注册到 `VisionConfigViewModel` 调度
- `IVisionFlowHandler` 的 `Execute()` 接收 `VisionFunction` 和 `VisionFlow`，通过 `func.Src` 访问图像
- 输出：写入 `func.Src`（修改后的图像），输出参数写入 `GlobalModel` 的局部变量表或全局变量，绘制通过 `func._DrawCommands` 列表（`DrawCommand` 类型）返回
- 局部变量写入模式：`handler 中 func.Flows[index].Flow.SetOutput("变量名", value)`，其中 `value` 应序列化为 `string`；VM 在 UI 线程通过 `App.Current.Dispatcher.Invoke(() => { item.Flow.Add("键", ...); })` 确保绑定更新

### ImageEdit 交互惯例

- **直线绘制**: 左键按下开始 → 拖动时橡皮筋预览 → 左键松开结束绘制 → 进入调整模式（拖端点/线段）
- **圆绘制**: 类似直线流程，左键松开固定圆心和半径 → 进入调整模式（点击圆心/圆周整体移动）
- **矩形/旋转矩形**: 左键拖动绘制 → 松开创建 → 调整模式
- **右键**: 在任何调整模式下右键点击 → 结束调整，确认 ROI 并返回结果给调用方
- **MouseCapture**: WPF 拖拽操作需显式调用 `Mouse.Capture(this)`，否则 `MouseUp` 可能丢失

## Notes

<!-- 快速添加入口 -->
