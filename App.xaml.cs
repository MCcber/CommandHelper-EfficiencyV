using Hardcodet.Wpf.TaskbarNotification;
using Serilog;
using System;
using System.Windows;

namespace cbhk_signin
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static TaskbarIcon TaskbarIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            SetupExceptionHandling();
            base.OnStartup(e);

        }

        /// <summary>
        /// Serilog捕获异常输出日志
        /// </summary>
        private void SetupExceptionHandling()
        {
            // 设置日志输出到文件
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + "Logs\\log.txt")
                .CreateLogger();

            // 全局异常监控
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                Log.Fatal(ev.ExceptionObject as Exception, "Oops!An exception occurred");
                Log.CloseAndFlush();
            };

            // 异常捕获监控
            DispatcherUnhandledException += (s, ev) =>
            {
                Log.Error(ev.Exception, "Oops!An UI exception occurred");
                ev.Handled = true;
                Log.CloseAndFlush();
            };
        }

        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjcwNzQ4MkAzMjMzMmUzMDJlMzBGWTltcUF3S0pNaC9ieDMxUWo5bFZjeTg0ZjM5OHc2MkxzQWNUMVVkKzNBPQ==");
        }
    }
}
