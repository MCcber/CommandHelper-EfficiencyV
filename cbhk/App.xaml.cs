using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using cbhk.Common;
using cbhk.GeneralTools;
using cbhk.GeneralTools.DataService;
using cbhk.Generators.DataPackGenerator;
using cbhk.Generators.DataPackGenerator.DatapackInitializationForms;
using cbhk.Generators.DimensionTypeGenerator;
using cbhk.Generators.EntityGenerator;
using cbhk.Generators.EntityGenerator.Components;
using cbhk.Generators.FireworkRocketGenerator;
using cbhk.Generators.ItemGenerator;
using cbhk.Generators.ItemGenerator.Components;
using cbhk.Generators.RecipeGenerator;
using cbhk.Generators.RecipeGenerator.Components;
using cbhk.Generators.SignGenerator;
using cbhk.Generators.SpawnerGenerator.Components;
using cbhk.Generators.VillagerGenerator;
using cbhk.Generators.VillagerGenerator.Components;
using cbhk.View;
using cbhk.View.Common;
using cbhk.View.Compoments.Spawner;
using cbhk.View.Generators;
using cbhk.ViewModel;
using cbhk.ViewModel.Common;
using cbhk.ViewModel.Components.Datapack.DatapackInitializationForms;
using cbhk.ViewModel.Components.Datapack.HomePage;
using cbhk.ViewModel.Components.Recipe;
using cbhk.ViewModel.Components.Villager;
using cbhk.ViewModel.Generators;
using cbhk.ViewModel.Generators.Datapack;
using Prism.DryIoc;
using Prism.Ioc;
using Serilog;

namespace cbhk
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

            //订阅异常处理
            SetupExceptionHandling();
            base.OnStartup(e);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            #region Service
            containerRegistry.RegisterSingleton<BlockService>();
            containerRegistry.RegisterSingleton<EntityService>();
            containerRegistry.RegisterSingleton<ItemService>();
            containerRegistry.RegisterSingleton<HtmlHelper>();
            #endregion

            #region View
            containerRegistry.RegisterSingleton<MoreView>();
            containerRegistry.RegisterForNavigation<MoreView,MoreViewModel>();
            containerRegistry.RegisterSingleton<NoticeToUsersView>();
            containerRegistry.RegisterForNavigation<NoticeToUsersView,NoticeToUsersViewModel>();
            containerRegistry.RegisterSingleton<SettingsView>();
            containerRegistry.RegisterForNavigation<SettingsView,SettingsViewModel>();
            containerRegistry.RegisterSingleton<TutorialsView>();
            containerRegistry.RegisterForNavigation<TutorialsView,TutorialsViewModel>();
            containerRegistry.RegisterSingleton<UpdateView>();
            containerRegistry.RegisterForNavigation<UpdateView, UpdateViewModel>();

            containerRegistry.RegisterSingleton<DistributorGenerator>();
            containerRegistry.RegisterSingleton<DisplayerView>();
            containerRegistry.RegisterForNavigation<DisplayerView, DisplayerViewModel>(Name.DisplayerView);
            containerRegistry.RegisterForNavigation<SignInView, SignInViewModel>(Name.SignInView);
            containerRegistry.RegisterSingleton<SignInView>();
            containerRegistry.RegisterForNavigation<MainView, MainViewModel>(Name.MainView);
            containerRegistry.RegisterSingleton<MainView>();
            containerRegistry.RegisterForNavigation<ArmorStandView, ArmorStandViewModel>(Name.ArmorStandView);
            containerRegistry.RegisterForNavigation<OnlyOneCommandView, OnlyOneCommandViewModel>(Name.OnlyOneCommandView);
            containerRegistry.RegisterForNavigation<TagView, TagViewModel>(Name.TagView);
            containerRegistry.RegisterForNavigation<WrittenBookView, WrittenBookViewModel>(Name.WrittenBookView);
            containerRegistry.RegisterForNavigation<SpawnerView,SpawnerViewModel>(Name.SpawnerView);
            containerRegistry.RegisterForNavigation<SpawnerPageView,SpawnerPageViewModel>(Name.SpawnerPageView);
            containerRegistry.RegisterForNavigation<DatapackView,DatapackViewModel>(Name.DatapackView);
            containerRegistry.RegisterForNavigation<EditPageView, EditPageViewModel>(Name.DatapackEditPageView);
            containerRegistry.RegisterForNavigation<DimensionTypeView,DimensionTypeViewModel>(Name.DimensionTypeView);
            containerRegistry.RegisterForNavigation<EntityView,EntityViewModel>(Name.EntityView);
            containerRegistry.RegisterForNavigation<EntityPagesView, EntityPagesViewModel>();
            containerRegistry.RegisterForNavigation<FireworkRocketView, FireworkRocketViewModel>(Name.FireworkRocketView);
            containerRegistry.RegisterForNavigation<ItemView,ItemViewModel>(Name.ItemView);
            containerRegistry.RegisterForNavigation<ItemPagesView, ItemPageViewModel>(Name.ItemView);
            containerRegistry.RegisterForNavigation<RecipeView,RecipeViewModel>(Name.RecipeView);
            containerRegistry.RegisterForNavigation<BlastFurnaceView,BlastFurnaceViewModel>(Name.BlastFurnaceView);
            containerRegistry.RegisterForNavigation<CampfireView,CampfireViewModel>(Name.CampfireView);
            containerRegistry.RegisterForNavigation<CraftingTableView,CraftingTableViewModel>(Name.CraftingTableView);
            containerRegistry.RegisterForNavigation<FurnaceView,FurnaceViewModel>(Name.FurnaceView);
            containerRegistry.RegisterForNavigation<SmithingTableView,SmithingTableViewModel>(Name.SmithingTableView);
            containerRegistry.RegisterForNavigation<SmokerView,SmokerViewModel>(Name.SmokerView);
            containerRegistry.RegisterForNavigation<StonecutterView, StonecutterViewModel>(Name.StonecutterView);
            containerRegistry.RegisterForNavigation<SignView,SignViewModel>(Name.SignInView);
            containerRegistry.RegisterForNavigation<VillagerView,VillagerViewModel>(Name.VillagerView);
            containerRegistry.RegisterForNavigation<TransactionItemsView,TransactionItemsViewModel>(Name.TransactionItemsView);
            containerRegistry.RegisterForNavigation<GossipsItemsView, GossipsItemsViewModel>(Name.GossipsItemsView);
            containerRegistry.RegisterForNavigation<DatapackGenerateSetupView,DatapackGenerateSetupViewModel>(Name.DatapackGenerateSetupView);
            containerRegistry.RegisterForNavigation<HomePageView,HomePageViewModel>(Name.HomePageView);
            containerRegistry.RegisterForNavigation<TemplateSelectPageView, TemplateSelectViewModel>(Name.TemplateSelectPageView);
            #endregion
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