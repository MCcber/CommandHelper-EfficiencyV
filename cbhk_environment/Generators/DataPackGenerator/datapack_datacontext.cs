using cbhk_environment.Generators.DataPackGenerator.DatapackInitializationForms;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace cbhk_environment.Generators.DataPackGenerator
{
    public class datapack_datacontext:ObservableObject
    {
        #region 主页，模板设置页，属性设置页，数据上下文的引用
        /// <summary>
        /// 分页框架
        /// </summary>
        public DependencyObject frame { get; set; } = null;
        /// <summary>
        /// 主页
        /// </summary>
        public HomePage homePage { get; set; } = new();
        /// <summary>
        /// 属性设置页
        /// </summary>
        public DatapackGenerateSetupPage datapackGenerateSetupPage { get; set; } = null;
        /// <summary>
        /// 模板选择页
        /// </summary>
        public TemplateSelectPage templateSelectPage { get; set; } = null;
        /// <summary>
        /// 编辑页
        /// </summary>
        public EditPage editPage { get; set; } = null;
        #endregion

        /// <summary>
        /// 处理关闭任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Datapack_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HomePageDataContext context = homePage.DataContext as HomePageDataContext;
            await context.ReCalculateSolutionPath();
        }
    }
}
