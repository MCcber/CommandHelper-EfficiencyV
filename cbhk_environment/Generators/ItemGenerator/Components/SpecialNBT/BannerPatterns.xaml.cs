using cbhk_environment.ControlsDataContexts;
using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk_environment.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// BannerPatterns.xaml 的交互逻辑
    /// </summary>
    public partial class BannerPatterns : UserControl
    {
        string patternKeysFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Item\\data\\BannerPatterns.ini";
        string patternImagesFolderPath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Item\\images\\BannerPatterns\\";
        string patternColorFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Item\\data\\BannerColors.ini";

        #region 合并数据
        public string Result
        {
            get
            {
                string result = "{Color:" + Color.SelectedIndex + (Pattern.SelectedItem != null? ",Pattern:\"" + (Pattern.SelectedItem as IconComboBoxItem).ComboBoxItemText : "") + "\"}";
                return result;
            }
        }
        #endregion

        public BannerPatterns()
        {
            InitializeComponent();
            List<string> patternKeys = File.ReadAllLines(patternKeysFilePath).ToList();
            List<IconComboBoxItem> patterns = new();
            for (int i = 0; i < patternKeys.Count; i++)
                patterns.Add(new IconComboBoxItem() { ComboBoxItemIcon = new BitmapImage(new Uri(patternImagesFolderPath + "white_banner_" + i + ".png", UriKind.Absolute)), ComboBoxItemText = patternKeys[i] });
            Color.ItemsSource = File.ReadAllLines(patternColorFilePath).ToList();
            Pattern.ItemsSource = patterns;
            Color.SelectedIndex = Pattern.SelectedIndex = 0;
        }

        /// <summary>
        /// 删除本控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IconTextButtons_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            StackPanel stackPanel = Parent as StackPanel;
            stackPanel.Children.Remove(this);
            stackPanel.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }
    }
}
