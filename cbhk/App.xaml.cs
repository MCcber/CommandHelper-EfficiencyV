using System;
using System.Diagnostics;
using System.IO;
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
        private static string StartUpTimeString = "";
        public void OnInstanceInvoked(string[] args)
        {
            
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {

        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Log.CloseAndFlush();
            File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"Logs\" + StartUpTimeString + ".txt");
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
            new SignIn().Show();
        }

        /// <summary>
        /// Serilog捕获异常输出日志
        /// </summary>
        private void SetupExceptionHandling()
        {
            StartUpTimeString = DateTime.Now.ToString("yyyy.MM.dd") + "\\" + DateTime.Now.ToString("HH.mm.ss");
            // 设置日志输出到文件
            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Fatal () // 设置最小日志记录级别为Error
                        .WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + @"Logs\" + StartUpTimeString + ".txt") // 设置日志输出文件路径
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
                if(ev.Exception.Message.Trim().Length > 0)
                {
                    Log.Error(ev.Exception, "Oops!An UI exception occurred");
                    ev.Handled = true;
                    Log.CloseAndFlush();
                }
            };
        }
    }

    public static partial class NativeMethods
    {
        [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial IntPtr FindWindow(string lpClassName, string lpWindowName);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}