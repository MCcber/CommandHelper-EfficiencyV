using CBHK.GeneralTool;
using CBHK.View;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace CBHK
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        private static string StartUpTimeString = "";

#pragma warning disable IDE0052
        private Mutex _mutex;
#pragma warning disable IDE0052

        /// <summary>
        /// 初始化数据与主窗体
        /// </summary>
        /// <returns></returns>
        protected override Window CreateShell()
        {
            ExternalDataImportManager.Init();
#if DEBUG
            return Container.Resolve<MainView>();
#else
            return Container.Resolve<SignInView>();
#endif
        }

        /// <summary>
        /// 将窗体设置为单例模式运行、订阅全局异常捕捉事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            const string applicationName = "CommandHelper-EfficiencyV @By HongHuangTaiChu Team";
            _mutex = new Mutex(true, applicationName, out bool isCreateNew);
            if (!isCreateNew)
            {
                MessageBox.Show("CommandHelper-EfficiencyV is already Running!", "CommandHelper-EfficiencyV", MessageBoxButton.OK, MessageBoxImage.Error);

                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                Current.Shutdown();
            }

            //订阅异常处理
            SetupExceptionHandling();
            base.OnStartup(e);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();

            // 自定义命名约定
            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType =>
            {
                var viewName = viewType.FullName;

                var viewModelName = viewName.Replace("CBHK.View", "CBHK.ViewModel") + "Model";

                return Type.GetType(viewModelName);
            });
        }

        /// <summary>
        /// Serilog捕获异常输出日志
        /// </summary>
        private void SetupExceptionHandling()
        {
            StartUpTimeString = DateTime.Now.ToString("yyyy.MM.dd") + "\\" + DateTime.Now.ToString("HH-mm-ss");
            // 设置日志输出到文件
            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Error() // 设置最小日志记录级别为Error
                        .WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + @"Logs\" + StartUpTimeString + ".txt", rollingInterval: RollingInterval.Hour) // 设置日志输出文件路径与回滚行为
                        .CreateLogger();

            // 全局异常监控
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                if ((ev.ExceptionObject as Exception).Message.Trim().Length > 0)
                {
                    Log.Fatal(ev.ExceptionObject as Exception, ev.ExceptionObject.ToString());
                    Log.CloseAndFlush();
                }
            };

            // 异常捕获监控
            DispatcherUnhandledException += (s, ev) =>
            {
                if (ev.Exception.Message.Trim().Length > 0)
                {
                    Log.Error(ev.Exception, ev.Exception.Message);
                    ev.Handled = true;
                    Log.CloseAndFlush();
                }
            };
        }
    }
}