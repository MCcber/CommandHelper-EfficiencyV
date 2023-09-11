using cbhk_environment.CustomControls;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// BannerBlockEntityTag.xaml 的交互逻辑
    /// </summary>
    public partial class BannerBlockEntityTag : UserControl
    {
        #region 合并数据
        public string Result
        {
            get
            {
                string result;
                string customNameResult = CustomName.Text.Length > 0 ? "CustomName:'{\"text\":\"" + CustomName.Text + "\"}'," : "";
                string patternResult = "";
                StackPanel stackPanel = (BannerPatternAccordion.Content as ScrollViewer).Content as StackPanel;
                foreach (BannerPatterns patterns in stackPanel.Children)
                    patternResult += patterns.Result + ",";
                patternResult = patternResult.Length > 0 ?"Patterns:[" + patternResult.Trim(',') + "]":"";
                result = (customNameResult.Length + patternResult.Length) > 0 ? "BlockEntityTag:{" + (customNameResult + patternResult).Trim(',') + "}" : "";
                return result;
            }
        }
        #endregion

        public BannerBlockEntityTag()
        {
            InitializeComponent();
            BannerPatternAccordion.Modify = new RelayCommand<FrameworkElement>(AddBannerPatternCommand);
            BannerPatternAccordion.Fresh = new RelayCommand<FrameworkElement>(ClearBannerPatternCommand);
        }

        /// <summary>
        /// 添加旗帜图案
        /// </summary>
        /// <param name="obj"></param>
        private void AddBannerPatternCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Add(new BannerPatterns());
        }

        /// <summary>
        /// 清空旗帜图案
        /// </summary>
        /// <param name="obj"></param>
        private void ClearBannerPatternCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Clear();
        }
    }
}
