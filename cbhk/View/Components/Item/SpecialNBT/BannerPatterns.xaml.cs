using cbhk.CustomControls;
using cbhk.GeneralTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// BannerPatterns.xaml 的交互逻辑
    /// </summary>
    public partial class BannerPatterns : UserControl
    {
        #region Field
        string patternKeysFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\ItemView\\data\\BannerPatterns.ini";
        string patternImagesFolderPath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\ItemView\\images\\BannerPatterns\\";
        string patternColorFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\ItemView\\data\\BannerColors.ini";
        #endregion

        #region Property
        public ObservableCollection<TextComboBoxItem> ColorList { get; set; } = [];
        #endregion

        #region 合并数据
        public string Result
        {
            get
            {
                string result = "{Color:" + Color.SelectedIndex + (Pattern.SelectedItem != null ? ",Pattern:\"" + (Pattern.SelectedItem as IconComboBoxItem).ComboBoxItemText : "") + "\"}";
                return result;
            }
        }
        #endregion

        public BannerPatterns()
        {
            InitializeComponent();
            List<string> patternKeys = File.ReadAllLines(patternKeysFilePath).ToList();
            List<IconComboBoxItem> patterns = [];
            for (int i = 0; i < patternKeys.Count; i++)
                patterns.Add(new IconComboBoxItem() { ComboBoxItemIcon = new BitmapImage(new Uri(patternImagesFolderPath + "white_banner_" + i + ".png", UriKind.Absolute)), ComboBoxItemText = patternKeys[i] });

            foreach (var item in File.ReadAllLines(patternColorFilePath).ToList())
            {
                ColorList.Add(new TextComboBoxItem() { Text = item });
            }
            Color.ItemsSource = ColorList;
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
