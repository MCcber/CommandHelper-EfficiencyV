using CBHK.Domain;
using CBHK.Model.Constant;
using CBHK.ViewModel.Generator;
using CBHK.Utility.Data;
using CBHK.View;
using DryIoc;
using MinecraftLanguageModelLibrary.Utility;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using CBHK.View.Component.Villager.Control;
using CBHK.ViewModel.Component.Villager.Control;
using CBHK.ViewModel.Component.Recipe.Strategy;

namespace CBHK
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        #region Field
        private static string StartUpTimeString = "";

#pragma warning disable IDE0052
        private Mutex mutex;
#pragma warning disable IDE0052
        #endregion

        #region Event

        /// <summary>
        /// 初始化数据与主窗体
        /// </summary>
        /// <returns></returns>
        protected override Window CreateShell()
        {
            return Container.Resolve<MainView>();
        }

        /// <summary>
        /// 将窗体设置为单例模式运行、订阅全局异常捕捉事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            const string applicationName = "CommandHelper-EfficiencyV @By HongHuangTaiChu Team";
            mutex = new Mutex(true, applicationName, out bool isCreateNew);
            if (!isCreateNew)
            {
                MessageBox.Show("CommandHelper-EfficiencyV is already Running!", "CommandHelper-EfficiencyV", MessageBoxButton.OK, MessageBoxImage.Error);

                var currentProcess = Process.GetCurrentProcess();
                _ = Process.GetProcessesByName(currentProcess.ProcessName);
                Current.Shutdown();
            }

            //订阅异常处理
            SetupExceptionHandling();

            base.OnStartup(e);

            // 全局字体：窗口样式里是 DynamicResource，改资源会传播到已经建好的窗体，
            // 所以放在 base 之后（外壳已创建）也能生效
            ApplyGlobalFont(Container.Resolve<EnvironmentSettingsService>().Current.FontFamily);
        }

        /// <summary>
        /// 把设置里的字体写进 GlobalFont 资源。字体名非法时保留 Visual.xaml 里的默认值。
        /// </summary>
        private void ApplyGlobalFont(string fontFamilyName)
        {
            if (string.IsNullOrWhiteSpace(fontFamilyName))
            {
                return;
            }

            try
            {
                Resources["GlobalFont"] = new FontFamily(fontFamilyName);
            }
            catch
            {
                // 名字解析不了就用默认字体，别让启动挂在字体上
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 数据源：仓内静态目录（Registries 的整理版）+ mcdoc 解析出来的命令枚举，两者都由本地文件驱动
            containerRegistry.RegisterSingleton<RegistryDataService>();
            containerRegistry.RegisterSingleton<SupplementDataService>();
            containerRegistry.RegisterSingleton<EnvironmentSettingsService>();
            containerRegistry.RegisterSingleton(typeof(ICatalogEnumProvider), typeof(CatalogEnumViewModel));

            containerRegistry.RegisterSingleton<Resource>();
            containerRegistry.RegisterSingleton<MainView>();
            containerRegistry.RegisterSingleton<DataService>();
            containerRegistry.RegisterSingleton<RegexService>();
            containerRegistry.RegisterSingleton<DisplayerView>();
            containerRegistry.RegisterSingleton<RecipeStrategyRegistry>(() => RecipeStrategyRegistry.Create());
            containerRegistry.RegisterSingleton<SlotDropStrategyRegistry>(() => SlotDropStrategyRegistry.Create());

            #region 注册页面上的组件
            containerRegistry.Register<TransactionItemView>();
            containerRegistry.Register<TransactionItemViewModel>();
            containerRegistry.Register<GossipsItemView>();
            containerRegistry.Register<GossipsItemViewModel>(); 
            #endregion

            // mcdoc 生成器页面：入口不同、引擎同一套（骨架阶段先注册为瞬时）
            containerRegistry.Register<AdvancementViewModel>();
            containerRegistry.Register<DimensionTypeViewModel>();
            containerRegistry.Register<DimensionViewModel>();
            containerRegistry.Register<PredicateViewModel>();
            containerRegistry.Register<LootTableViewModel>();
            containerRegistry.Register<ItemModifierViewModel>();
            containerRegistry.Register<DamageTypeViewModel>();
            containerRegistry.Register<ChatTypeViewModel>();
            containerRegistry.Register<CustomWorldGeneratorViewModel>();
        }

        /// <summary>
        /// 重写Prism的定位器
        /// </summary>
        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();

            // 自定义命名约定
            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType =>
            {
                var viewName = viewType.FullName;

                var viewModelName = viewName.Replace("view", "ViewModel").Replace("View", "ViewModel");

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
                var fatal = ev.ExceptionObject as Exception;
                if (fatal?.Message.Trim().Length > 0)
                {
                    Log.Fatal(fatal, fatal.ToString());
                    // 进程马上就要结束：这是最后一次落盘机会
                    Log.CloseAndFlush();
                }
            };

            // 异常捕获监控
            DispatcherUnhandledException += (s, ev) =>
            {
                // 分闸落一份带调用栈的证据：Serilog 只留 Error 以上，mcdoc 现场还要看得见是哪条链路
                CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Error("DispatcherUnhandledException", ev.Exception);
                if (ev.Exception.Message.Trim().Length > 0)
                {
                    Log.Error(ev.Exception, ev.Exception.Message);
                    ev.Handled = true;
                    // 不许在这里 CloseAndFlush：关掉之后第二次异常一条都记不下来（日志静默失效）
                }
            };
        }
        #endregion
    }
}