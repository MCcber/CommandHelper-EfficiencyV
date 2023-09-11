using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.SettingForm
{
    /// <summary>
    /// IndividualizationForm.xaml 的交互逻辑
    /// </summary>
    public partial class IndividualizationForm
    {
        List<FontStructure> fontStructureList = new();
        PrivateFontCollection privateFontCollection = new();
        InstalledFontCollection systemFonts = new();
        List<string> fontList = new();

        public IndividualizationForm()
        {
            InitializeComponent();


            #region 获取自定义字体库
            string fontListDirectory = AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts";
            string[] fontListFolder = Directory.GetFiles(fontListDirectory,"*ttf",SearchOption.AllDirectories);
            foreach (string fontFile in fontListFolder)
            {
                bool HaveFamily = privateFontCollection.Families.Any(item=>item.Name == Path.GetFileNameWithoutExtension(fontFile));
                if(!HaveFamily)
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
            fontFamily = privateFontCollection.Families.Where(item=>item.Name == selectedFont).First();
            if (fontFamily == null)
                fontFamily = new(fontList[FontBox.SelectedIndex]);

            try
            {
                if (FontBox.SelectedIndex < fontStructureList.Count && File.Exists(fontStructureList[FontBox.SelectedIndex].FilePath))
                    File.Copy(fontStructureList[FontBox.SelectedIndex].FilePath, AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts\\Selected.ttf", true);
                else
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts\\Selected.txt", (FontBox.SelectedIndex - privateFontCollection.Families.Length).ToString(), System.Text.Encoding.UTF8);
            }
            catch { }
        }

        /// <summary>
        /// 打开轮播图设置窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetLinks(object sender, RoutedEventArgs e)
        {
            SetRoatationChart setRoatationChart = new();
            if(setRoatationChart.ShowDialog() == true)
            {

            }
        }

        private void SettingForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
        }
    }


    public class FontStructure
    {
        public string FontName { get; set; }
        public string FilePath { get; set; }
    }
}
