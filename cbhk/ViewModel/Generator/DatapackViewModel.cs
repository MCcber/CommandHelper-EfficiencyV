using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System;
using System.Windows;
using CBHK.View;
using CBHK.ViewModel.Component.Datapack.HomePage;
using CBHK.View.Component.Datapack.EditPage;
using CBHK.View.Component.Datapack.HomePage;
using CBHK.View.Component.Datapack.DatapackInitializationForms;

namespace CBHK.ViewModel.Generator
{
    public partial class DatapackViewModel : ObservableObject
    {
        #region Field
        /// <summary>
        /// 主页
        /// </summary>
        private MainView home = null;
        #endregion

        #region Property
        /// <summary>
        /// 分页框架
        /// </summary>
        [ObservableProperty]
        private DependencyObject _frame = null;
        /// <summary>
        /// 主页
        /// </summary>
        [ObservableProperty]
        private HomePageView _homePage = new();
        /// <summary>
        /// 属性设置页
        /// </summary>
        [ObservableProperty]
        private DatapackGenerateSetupPageView _datapackGenerateSetupPage = null;
        /// <summary>
        /// 模板选择页
        /// </summary>
        [ObservableProperty]
        private TemplateSelectPageView _templateSelectPage = null;
        /// <summary>
        /// 编辑页
        /// </summary>
        [ObservableProperty]
        private EditPageView _editPage = null;
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
            HomePageViewModel context = HomePage.DataContext as HomePageViewModel;
            await context.ReCalculateSolutionPath();
        }
    }
}