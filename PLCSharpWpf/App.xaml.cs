using DryIoc;
using Newtonsoft.Json;
using PLCSharp.Core.Prism;
using PLCSharp.Models;
using PLCSharp.VVMs.Authority;
using PLCSharp.VVMs.MainWindow;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;


namespace PLCSharp
{
    /// <summary>
    /// 应用程序入口，负责初始化 Prism 框架和注册组件
    /// </summary>
    public partial class App
    {
        private Mutex mutex;

        //第1执行
        /// <summary>
        /// 注册应用程序所需的类型和服务
        /// </summary>
        /// <param name="containerRegistry">Prism 容器注册器</param>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            mutex = new Mutex(true, "PLCSharp", out bool ret);

            if (!ret)
            {
                ForegroundWindow();
                Environment.Exit(0);
            }
            //数据库
            _ = containerRegistry.RegisterSingleton<DatasContext>();
            _ = Container.Resolve<DatasContext>();

            RegisterEvents(); // 捕获全局异常
            LoadConfig();

            //Module中的初始化晚于这里，所以不能在Module中注册全局使用的实例，比如NavigateModel

            //注册Model特性的类，单例

            var models = from t in Assembly.GetExecutingAssembly().GetTypes()
                         where t.GetCustomAttribute<ModelAttribute>() is not null
                         select t;

            foreach (var model in models)
            {
                _ = containerRegistry.RegisterSingleton(model);
            }


            //注册导航特性的类到导航
            var views = from t in Assembly.GetExecutingAssembly().GetTypes()
                        where t.GetCustomAttribute<NavigationPageAttribute>() is not null
                        select t;
            foreach (var view in views)
            {
                containerRegistry.RegisterForNavigation(view, view.Name);
            }

            //注册弹出窗口特性的类，到导航
            var dialogs = from t in Assembly.GetExecutingAssembly().GetTypes()   //
                          where t.GetCustomAttribute<DialogAttribute>() is not null
                          select t;
            foreach (var dialog in dialogs)
            {
                containerRegistry.RegisterForNavigation(dialog, dialog.Name);
            }

            //反射要加入菜单的弹出窗口特性的类，注册到导航
            var dialogsMenu = from t in Assembly.GetExecutingAssembly().GetTypes()   //
                              where t.GetCustomAttribute<DialogMenuAttribute>() is not null
                              select t;
            foreach (var dialog in dialogsMenu)
            {
                containerRegistry.RegisterForNavigation(dialog, dialog.Name);
            }



        }

        //第2执行
        /// <summary>
        /// 配置模块目录
        /// </summary>
        /// <param name="moduleCatalog">模块目录</param>
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            //_ = moduleCatalog.AddModule<PLCSharp.CoreModule>();
        }

        //第3执行
        /// <summary>
        /// 创建Shell
        /// </summary>
        /// <returns>返回结果</returns>
        protected override Window CreateShell()
        {
            //反射需要导航的页面
            var navigate = Container.Resolve<NavigateModel>();

            var views = from t in Assembly.GetExecutingAssembly().GetTypes()  //
                        where t.GetCustomAttribute<NavigationPageAttribute>() is not null
                        select t;

            foreach (var view in views)
            {
                var page = view.GetCustomAttribute<NavigationPageAttribute>();

                var navigteItem = Container.Resolve<NavigateItem>();
                navigteItem.ViewName = page.ViewName;
                navigteItem.IconKind = page.IconKind;
                navigteItem.DisplayName = page.DisplayName;
                navigteItem.UserLevel = page.UserLevel;
                navigteItem.Index = page.Index;
                navigate.NavigateList.Add(navigteItem);


            }
            //反射需要弹出的窗口
            var dialogs = from t in Assembly.GetExecutingAssembly().GetTypes()
                          where t.GetCustomAttribute<DialogMenuAttribute>() is not null
                          select t;
            foreach (var dialog in dialogs)
            {

                var page = dialog.GetCustomAttribute<DialogMenuAttribute>();
                var navigteItem = Container.Resolve<NavigateItem>();
                navigteItem.ViewName = page.ViewName;
                navigteItem.IconKind = page.IconKind;
                navigteItem.DisplayName = page.DisplayName;
                navigteItem.UserLevel = page.UserLevel;
                navigteItem.Index = page.Index;
                navigate.DialogList.Add(navigteItem);

            }

            var window = Container.Resolve<MainWindow>();

            var user = Container.Resolve<LoginModel>();
#if DEBUG
            user.CurrentUser = user.UserList.Where(u => u.Name == "admin").FirstOrDefault();

#else
            user.CurrentUser = user.UserList.Where(u => u.Name == "guest").FirstOrDefault();

#endif

            //设置默认页面
            navigate.DefaultView = "Homepage";
            _ = navigate.NavigateToAsync(navigate.DefaultView);

            //启动主窗体

            return window;
        }
        /// <summary>
        /// 自动寻找ViewModel并绑定到View，要求ViewModel命名为View的全名加上ViewModel，比如MainWindow的ViewModel命名为MainWindowViewModel
        /// </summary>
        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();

            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver((viewType) =>
            {
                var viewName = viewType.FullName;
                var viewAssemblyName = viewType.GetTypeInfo().Assembly.FullName;
                var viewModelName = $"{viewName}ViewModel, {viewAssemblyName}";
                return Type.GetType(viewModelName);
            });
        }
        /// <summary>
        /// 加载环境变量
        /// </summary>
        private static void LoadConfig()
        {

            if (!Directory.Exists("./Config/"))
            {
                Directory.CreateDirectory("./Config/");
            }

            if (File.Exists("./Config/EnvironmentVariable.json"))
            {
                var json = File.ReadAllText("./Config/EnvironmentVariable.json");
                dynamic config = JsonConvert.DeserializeObject(json);
                try
                {
                    foreach (var item in config.EnvironmentVariables)
                    {
                        string name = item.Name;
                        string value = item.Value;
                        Environment.SetEnvironmentVariable(name, value);
                    }
                }
                catch (Exception)
                {


                }

            }


        }


        [LibraryImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        private static partial bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

        [DllImport("User32.dll")]

        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Foreground窗口
        /// </summary>
        public static void ForegroundWindow()
        {

            Process currentProcess = Process.GetCurrentProcess();

            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {

                    // 将现有实例的窗口切换到前台

                    IntPtr hWnd = process.MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {

                        ShowWindowAsync(hWnd, 9);

                        SetForegroundWindow(hWnd);

                    }
                    break;
                }
            }
        }



        private void RegisterEvents()
        {
            // 后台线程异常的捕获方法 
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {

                RecordLog(args.Exception.ToString());

                args.SetObserved();
            };
            // 全局异常的捕获方法 
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {

                RecordLog(args.ExceptionObject.ToString());

            };
        }
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="showMag">显示Mag</param>
        public void RecordLog(string showMag)
        {
            lock (this)
            {

                var path = Environment.CurrentDirectory + @"\DebugLog\";  //文件夹路径

                var paths = path + DateTime.Now.ToString("yyyyMMdd") + ".txt";  //文件路径  以年月日为名称

                //创建文件夹
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                //打开或创建文件
                var fs = File.Open(paths, FileMode.Append);

                //获得字节数组

                var data = System.Text.Encoding.Default.GetBytes(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "\r\n" + showMag + "\r\n");
                //开始写入
                fs.Write(data, 0, data.Length);
                //清空缓冲区、关闭流
                fs.Flush();
                fs.Close();
            }

        }
    }
}
