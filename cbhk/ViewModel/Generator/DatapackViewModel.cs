using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System;
using System.Windows;
using CBHK.View;
using CBHK.ViewModel.Component.Datapack.HomePage;
using CBHK.View.Component.Datapack.EditPage;
using CBHK.View.Component.DatapackInitializationForms;
using CBHK.View.Component.Datapack.HomePage;

namespace CBHK.ViewModel.Generator
{
    public class DatapackViewModel : ObservableObject
    {
        #region 主页，模板设置页，属性设置页，数据上下文的引用
        /// <summary>
        /// 分页框架
        /// </summary>
        public DependencyObject frame { get; set; } = null;
        /// <summary>
        /// 主页
        /// </summary>
        public HomePageView homePage { get; set; } = new();
        /// <summary>
        /// 属性设置页
        /// </summary>
        public DatapackGenerateSetupView datapackGenerateSetupPage { get; set; } = null;
        /// <summary>
        /// 模板选择页
        /// </summary>
        public TemplateSelectPageView templateSelectPage { get; set; } = null;
        /// <summary>
        /// 编辑页
        /// </summary>
        public EditPageView editPage { get; set; } = null;
        /// <summary>
        /// 主页
        /// </summary>
        public MainView home = null;
        #endregion

        #region 语言服务器线程
        Process serverProcess = new();
        ProcessStartInfo startInfo = new()
        {
            // 设置不在新窗口中启动新的进程
            CreateNoWindow = true,
            UseShellExecute = false
        };
        #endregion

        public DatapackViewModel()
        {
            startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "AutoCompleteServer.exe";
            serverProcess = Process.Start(startInfo);
        }

        /// <summary>
        /// 处理关闭任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Datapack_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            serverProcess.Kill();
            HomePageViewModel context = homePage.DataContext as HomePageViewModel;
            await context.ReCalculateSolutionPath();
        }
    }
}