using System.Text;
using System.Threading.Tasks;
using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.IO;
using System.Drawing.Text;

namespace cbhk_environment
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static string targetFamilyFilePath { get; set; } = AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts\\Selected.ttf";
        public static string targetSystemFamilyIndexFilePath { get; set; } = AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts\\Selected.txt";

        static App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjY2ODU3MUAzMjMyMmUzMDJlMzBqY25rMHdhOHVCV056R2RXYTRscUhrb0JQNmNQSXFVcnFFOGRnVUt4b3NVPQ==");
            if (File.Exists(targetFamilyFilePath))
            {
                FontFamily family = new(targetFamilyFilePath);
                TextElement.FontFamilyProperty.OverrideMetadata(typeof(TextElement), new FrameworkPropertyMetadata(family));
                TextBlock.FontFamilyProperty.OverrideMetadata(typeof(TextBlock), new FrameworkPropertyMetadata(family));
            }
            else
            if(File.Exists(targetSystemFamilyIndexFilePath))
            {
                InstalledFontCollection fontCollection = new();
                FontFamily fontFamily = new(fontCollection.Families[int.Parse(File.ReadAllText(targetSystemFamilyIndexFilePath))].Name);
                TextElement.FontFamilyProperty.OverrideMetadata(typeof(TextElement), new FrameworkPropertyMetadata(fontFamily));
                TextBlock.FontFamilyProperty.OverrideMetadata(typeof(TextBlock), new FrameworkPropertyMetadata(fontFamily));
            }
        }
        void App_Startup(object sender, StartupEventArgs e)
        {
            //UI线程未捕获异常处理事件
            DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            //非UI线程未捕获异常处理事件
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        void App_Exit(object sender, ExitEventArgs e)
        {
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                e.Handled = true; //把 Handled 属性设为true，表示此异常已处理，程序可以继续运行，不会强制退出      
            }
            catch (Exception ex)
            {
                //此时程序出现严重异常，将强制结束退出
                System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "test.log", ex.Message);
                MessageBox.Show("UI线程发生致命错误！");
            }

        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            StringBuilder sbEx = new StringBuilder();
            if (e.IsTerminating)
            {
                sbEx.Append("非UI线程发生致命错误");
            }
            sbEx.Append("非UI线程异常：");
            if (e.ExceptionObject is Exception)
            {
                sbEx.Append(((Exception)e.ExceptionObject).Message);
            }
            else
            {
                sbEx.Append(e.ExceptionObject);
            }
            MessageBox.Show(sbEx.ToString());
        }

        void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            //task线程内未处理捕获
            MessageBox.Show("Task线程异常：" + e.Exception.Message);
            e.SetObserved();//设置该异常已察觉（这样处理后就不会引起程序崩溃）
        }
    }
}
