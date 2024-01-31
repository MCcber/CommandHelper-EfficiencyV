using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.IO;
using System.Linq;
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
        ObservableCollection<TextComboBoxItem> CurrentFontFamilyNameList { get; set; } = [];
        List<FontFamily> CurrentFontFamilyList { get; set; } = [];

        public Settings()
        {
            InitializeComponent();
            StateComboBox.ItemsSource = new ObservableCollection<TextComboBoxItem>() { 
                new() { Text = "保持不变" },
                new() { Text = "最小化" },
                new() { Text = "关闭" }
            };

            #region 获取自定义字体库
            string[] fontListFolder = Directory.GetFiles(fontListDirectory, "*ttf", SearchOption.AllDirectories);
            List<string> fontNameList = [];
            foreach (string fontFile in fontListFolder)
            {
                FontFamily family = new(Path.GetFileNameWithoutExtension(fontFile));
                if (fontNameList.Count > 0 && fontNameList.Contains(family.FamilyNames.First().Value))
                    continue;
                fontNameList.Add(family.FamilyNames.First().Value);
                TextComboBoxItem textComboBoxItem = new() 
                { 
                    Text = new FontFamily(Path.GetFileNameWithoutExtension(fontFile)).FamilyNames.First().Value 
                };
                if (!CurrentFontFamilyNameList.Contains(textComboBoxItem))
                {
                    CurrentFontFamilyNameList.Add(textComboBoxItem);
                    CurrentFontFamilyList.Add(family);
                    textComboBoxItem.ItemFont = family;
                }
            }
            #endregion

            #region 获取系统自带的字体库
            foreach (System.Drawing.FontFamily font in SystemFonts.Families)
            {
                FontFamily family = new(font.Name);
                CurrentFontFamilyList.Add(family);
                TextComboBoxItem textComboBoxItem = new() { Text = font.Name };
                CurrentFontFamilyNameList.Add(textComboBoxItem);
                textComboBoxItem.ItemFont = family;
            }
            #endregion

            #region 绑定字体数据源，更新托盘状态
            FontBox.ItemsSource = CurrentFontFamilyNameList;
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
        private void FontBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => FontFamily = CurrentFontFamilyList[FontBox.SelectedIndex];

        private void CloseToTray_Click(object sender, RoutedEventArgs e) => MainWindowProperties.CloseToTray = (sender as CheckBox).IsChecked.Value;
    }
}