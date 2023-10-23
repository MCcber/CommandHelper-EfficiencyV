using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.CustomControls
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : UserControl
    {
        List<FontStructure> fontStructureList = new();
        PrivateFontCollection privateFontCollection = new();
        InstalledFontCollection systemFonts = new();
        List<string> fontList = new();

        public Settings()
        {
            InitializeComponent();
            StateComboBox.ItemsSource = new ObservableCollection<string> { "保持不变", "最小化", "关闭" };

            #region 获取自定义字体库
            string fontListDirectory = AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts";
            string[] fontListFolder = Directory.GetFiles(fontListDirectory, "*ttf", SearchOption.AllDirectories);
            foreach (string fontFile in fontListFolder)
            {
                bool HaveFamily = privateFontCollection.Families.Any(item => item.Name == Path.GetFileNameWithoutExtension(fontFile));
                if (!HaveFamily)
                {
                    privateFontCollection.AddFontFile(fontFile);
                    if (!fontList.Contains(privateFontCollection.Families[^1].Name))
                        fontList.Add(privateFontCollection.Families[^1].Name);
                    fontStructureList.Add(new FontStructure() { FontName = privateFontCollection.Families[^1].Name, FilePath = fontFile });
                }
            }
            #endregion

            #region 获取系统自带的字体库
            foreach (FontFamily font in systemFonts.Families)
            {
                fontStructureList.Add(new FontStructure() { FontName = font.Name });
                fontList.Add(font.Name);
            }
            #endregion

            FontBox.ItemsSource = fontList;
            FontBox.SelectedIndex = 0;
            FontBox.SelectionChanged += FontBox_SelectionChanged;
            CloseToTray.IsChecked = MainWindowProperties.CloseToTray;
        }

        /// <summary>
        /// 复制选中的字体文件到指定路径下的指定文件名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FontBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedFont = FontBox.SelectedItem.ToString();
            FontFamily fontFamily = null;
            fontFamily = privateFontCollection.Families.Where(item => item.Name == selectedFont).First();
            fontFamily ??= new(fontList[FontBox.SelectedIndex]);

            try
            {
                if (FontBox.SelectedIndex < fontStructureList.Count && File.Exists(fontStructureList[FontBox.SelectedIndex].FilePath))
                    File.Copy(fontStructureList[FontBox.SelectedIndex].FilePath, AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts\\Selected.ttf", true);
                else
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts\\Selected.txt", (FontBox.SelectedIndex - privateFontCollection.Families.Length).ToString(), System.Text.Encoding.UTF8);
            }
            catch { }
        }

        private void CloseToTray_Click(object sender, RoutedEventArgs e)
        {
            MainWindowProperties.CloseToTray = (sender as CheckBox).IsChecked.Value;
        }
    }

    public class FontStructure
    {
        public string FontName { get; set; }
        public string FilePath { get; set; }
    }
}
