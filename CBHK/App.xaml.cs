using CBHK.Common.Utility;
using CBHK.Domain;
using CBHK.Utility.Common;
using CBHK.View;
using Microsoft.EntityFrameworkCore;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using Serilog;
using SQLitePCL;
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
        #region Field
        private static string StartUpTimeString = "";

        public static IServiceProvider ServiceProvider { get; private set; }

#pragma warning disable IDE0052
        private Mutex _mutex;
#pragma warning disable IDE0052
        #endregion

        #region Event
        /// <summary>
        /// 初始化数据与主窗体
        /// </summary>
        /// <returns></returns>
        protected override Window CreateShell()
        {
            ExternalDataImportManager.Init();
            return Container.Resolve<MainView>();
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

            //必须步骤，为了让SQLite的代码先行模式能够正常运行
            Batteries.Init();

            //订阅异常处理
            SetupExceptionHandling();
            base.OnStartup(e);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterScoped<CBHKDataContext>(() =>
            {
                var options = new DbContextOptionsBuilder<CBHKDataContext>()
                    .Options;

                return new CBHKDataContext(options);
            });

            containerRegistry.RegisterSingleton<DataService>();
            containerRegistry.RegisterSingleton<RegexService>();
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
        #endregion
    }
}