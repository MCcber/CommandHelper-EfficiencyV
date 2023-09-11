using Hardcodet.Wpf.TaskbarNotification;
using NLog;
using System;
using System.Threading.Tasks;
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
            base.OnStartup(e);
            SetupExceptionHandling();
        }

        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjY2ODU3MUAzMjMyMmUzMDJlMzBqY25rMHdhOHVCV056R2RXYTRscUhrb0JQNmNQSXFVcnFFOGRnVUt4b3NVPQ==");
        }

        ////首先注册开始和退出事件
        //public App()
        //{
        //    //Startup += new StartupEventHandler(App_Startup);
        //    //Exit += new ExitEventHandler(App_Exit);
        //}
        //void App_Startup(object sender, StartupEventArgs e)
        //{
        //    //UI线程未捕获异常处理事件
        //    DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
        //    //Task线程内未捕获异常处理事件
        //    TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        //    //非UI线程未捕获异常处理事件
        //    AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        //}

        //void App_Exit(object sender, ExitEventArgs e)
        //{
        //}

        //void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        //{
        //    try
        //    {
        //        e.Handled = true; //把 Handled 属性设为true，表示此异常已处理，程序可以继续运行，不会强制退出      
        //    }
        //    catch (Exception ex)
        //    {
        //        //此时程序出现严重异常，将强制结束退出
        //        System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "test.log", ex.Message);
        //        MessageBox.Show("UI线程发生致命错误！");
        //    }

        //}

        //void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    StringBuilder sbEx = new StringBuilder();
        //    if (e.IsTerminating)
        //    {
        //        sbEx.Append("非UI线程发生致命错误");
        //    }
        //    sbEx.Append("非UI线程异常：");
        //    if (e.ExceptionObject is Exception)
        //    {
        //        sbEx.Append(((Exception)e.ExceptionObject).Message);
        //    }
        //    else
        //    {
        //        sbEx.Append(e.ExceptionObject);
        //    }
        //    MessageBox.Show(sbEx.ToString());
        //}

        //void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        //{
        //    //task线程内未处理捕获
        //    MessageBox.Show("Task线程异常：" + e.Exception.Message);
        //    e.SetObserved();//设置该异常已察觉（这样处理后就不会引起程序崩溃）
        //}

        #region 抓捕异常
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
                _logger.Error(exception.StackTrace);
            }
            catch
            {
                //_logger.Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                //_logger.Error(exception, message);
            }
        }
        #endregion
    }
}
