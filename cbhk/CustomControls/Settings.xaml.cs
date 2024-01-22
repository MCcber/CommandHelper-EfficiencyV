using System;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk.CustomControls
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : UserControl
    {
        InstalledFontCollection SystemFonts = new();
        string fontListDirectory = AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts";
        ObservableCollection<FontFamily> CurrentFontFamilies { get; set; } = [];

        public Settings()
        {
            InitializeComponent();
            StateComboBox.ItemsSource = new ObservableCollection<string> { "保持不变", "最小化", "关闭" };

            #region 获取自定义字体库
            string[] fontListFolder = Directory.GetFiles(fontListDirectory, "*ttf", SearchOption.AllDirectories);
            foreach (string fontFile in fontListFolder)
                CurrentFontFamilies.Add(new FontFamily(Path.GetFileNameWithoutExtension(fontFile)));
            #endregion

            #region 获取系统自带的字体库
            foreach (System.Drawing.FontFamily font in SystemFonts.Families)
                CurrentFontFamilies.Add(new FontFamily(font.Name));
            #endregion

            #region 绑定字体数据源，更新托盘状态
            FontBox.ItemsSource = CurrentFontFamilies;
            FontBox.SelectedIndex = 0;
            FontBox.SelectionChanged += FontBox_SelectionChanged;
            CloseToTray.IsChecked = MainWindowProperties.CloseToTray;
            #endregion
        }

        /// <summary>
        /// 复制选中的字体文件到指定路径下的指定文件名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => FontFamily = CurrentFontFamilies[FontBox.SelectedIndex];

        private void CloseToTray_Click(object sender, RoutedEventArgs e) => MainWindowProperties.CloseToTray = (sender as CheckBox).IsChecked.Value;
    }
}
