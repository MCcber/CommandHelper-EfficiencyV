using cbhk_environment.CustomControls;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// ShieldBlockEntityTag.xaml 的交互逻辑
    /// </summary>
    public partial class ShieldBlockEntityTag : UserControl
    {
        string bannerColorFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Item\\data\\BannerColors.ini";

        #region 合并数据
        public string Result
        {
            get
            {
                string result;
                string BlockEntityTag = "";
                StackPanel stackPanel = (BannerAccordion.Content as ScrollViewer).Content as StackPanel;
                if (stackPanel.Children.Count > 0)
                    BlockEntityTag = "," + (stackPanel.Children[0] as BannerBlockEntityTag).Result;
                result = "Base:" + Base.SelectedIndex + BlockEntityTag;
                return result.Trim(',');
            }
        }
        #endregion

        public ShieldBlockEntityTag()
        {
            InitializeComponent();
            BannerAccordion.Modify = new RelayCommand<FrameworkElement>(AddBannerCommand);
            BannerAccordion.Fresh = new RelayCommand<FrameworkElement>(ClearBannerCommand);
            List<string> colorList = File.ReadAllLines(bannerColorFilePath).ToList();
            Base.ItemsSource = colorList;
            Base.SelectedIndex = 0;
        }

        /// <summary>
        /// 添加旗帜
        /// </summary>
        /// <param name="obj"></param>
        private void AddBannerCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            if(stackPanel.Children.Count == 0)
            stackPanel.Children.Add(new BannerBlockEntityTag());
        }

        /// <summary>
        /// 清空旗帜
        /// </summary>
        /// <param name="obj"></param>
        private void ClearBannerCommand(FrameworkElement obj)
        {
            Accordion accordion = obj as Accordion;
            StackPanel stackPanel = (accordion.Content as ScrollViewer).Content as StackPanel;
            stackPanel.Children.Clear();
        }
    }
}
