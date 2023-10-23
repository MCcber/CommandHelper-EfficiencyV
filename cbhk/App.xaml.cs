using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Serilog;
using SingleInstanceCore;

namespace cbhk
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application,ISingleInstance
    {
        public void OnInstanceInvoked(string[] args)
        {
            Exit += App_Exit;   
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            SingleInstance.Cleanup();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool isFirstInstance = this.InitializeAsFirstInstance("CommandHelper-EfficiencyV");
            if (!isFirstInstance)
            {
                MessageBox.Show("CommandHelper-EfficiencyV is already Running!", "CommandHelper-EfficiencyV", MessageBoxButton.OK, MessageBoxImage.Error);

                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var process in processes)
                {
                    if (process.Id != currentProcess.Id && process.MainModule.FileName == currentProcess.MainModule.FileName)
                    {
                        IntPtr hWnd = NativeMethods.FindWindow(null, process.MainWindowTitle);
                        NativeMethods.ShowWindow(hWnd, 9); // SW_RESTORE = 9
                        NativeMethods.SetForegroundWindow(hWnd);
                        break;
                    }
                }

                Current.Shutdown();
            }

            SetupExceptionHandling();
            base.OnStartup(e);
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NHaF5cXmpCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdgWH9edXRSRmBdVUR2WEs=");
        }

        /// <summary>
        /// Serilog捕获异常输出日志
        /// </summary>
        private void SetupExceptionHandling()
        {
            // 设置日志输出到文件
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + "Logs\\"+DateTime.Now.ToString("yyyy.MM.dd")+"\\"+DateTime.Now.ToString("HH-mm-ss") +".txt")
                .CreateLogger();

            // 全局异常监控
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                if((ev.ExceptionObject as Exception).Message.Trim().Length > 0)
                {
                    Log.Fatal(ev.ExceptionObject as Exception, "Oops!An exception occurred");
                    Log.CloseAndFlush();
                }
            };

            // 异常捕获监控
            DispatcherUnhandledException += (s, ev) =>
            {
                if(ev.Exception.Message.Length > 0)
                {
                    Log.Error(ev.Exception, "Oops!An UI exception occurred");
                    ev.Handled = true;
                    Log.CloseAndFlush();
                }
            };
        }
    }

    public static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
