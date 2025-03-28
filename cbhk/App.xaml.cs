using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using CBHK.Common;
using CBHK.GeneralTools;
using CBHK.Generators.DataPackGenerator;
using CBHK.Generators.DataPackGenerator.DatapackInitializationForms;
using CBHK.Generators.DimensionTypeGenerator;
using CBHK.Generators.EntityGenerator;
using CBHK.Generators.EntityGenerator.Components;
using CBHK.Generators.FireworkRocketGenerator;
using CBHK.Generators.ItemGenerator;
using CBHK.Generators.ItemGenerator.Components;
using CBHK.Generators.RecipeGenerator;
using CBHK.Generators.RecipeGenerator.Components;
using CBHK.Generators.SignGenerator;
using CBHK.Generators.SpawnerGenerator.Components;
using CBHK.Generators.VillagerGenerator;
using CBHK.Generators.VillagerGenerator.Components;
using CBHK.View;
using CBHK.View.Common;
using CBHK.View.Compoments.Spawner;
using CBHK.View.Components.Datapack.EditPage;
using CBHK.View.Generators;
using CBHK.ViewModel;
using CBHK.ViewModel.Common;
using CBHK.ViewModel.Components.Datapack.DatapackInitializationForms;
using CBHK.ViewModel.Components.Datapack.EditPage;
using CBHK.ViewModel.Components.Datapack.HomePage;
using CBHK.ViewModel.Components.Recipe;
using CBHK.ViewModel.Components.Villager;
using CBHK.ViewModel.Generators;
using Prism.DryIoc;
using Prism.Ioc;
using Serilog;

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

            containerRegistry.RegisterForNavigation<AdvancementView, AdvancementViewModel>(Name.AdvancementView);
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
}