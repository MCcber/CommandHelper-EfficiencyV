using cbhk.CustomControls;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System;
using System.Windows.Media;
using cbhk.Model;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows;

namespace cbhk.ViewModel.Common
{
    public partial class SettingsViewModel: ObservableObject
    {
        #region Field
        InstalledFontCollection SystemFonts = new();
        string fontListDirectory = AppDomain.CurrentDomain.BaseDirectory + "Resource\\Fonts";
        #endregion

        #region Property
        public ObservableCollection<TextComboBoxItem> CurrentFontFamilyNameList { get; set; } = [];
        [ObservableProperty]
        public int _selectVisibleIndex = 0;

        [ObservableProperty]
        public int _selectFontIndex = 0;

        [ObservableProperty]
        private TextComboBoxItem _selectedFontFamilyItem;

        [ObservableProperty]
        private FontFamily _selectedFontFamily;

        List<FontFamily> CurrentFontFamilyList { get; set; } = [];

        public ObservableCollection<TextComboBoxItem> StateList { get; set; } = [
                new() { Text = "保持不变" },
                new() { Text = "最小化" },
                new() { Text = "关闭" }
            ];

        private bool closeToTray = MainWindowProperties.CloseToTray;

        public bool CloseToTray
        {
            get => closeToTray;
            set
            {
                SetProperty(ref closeToTray, value);
                MainWindowProperties.CloseToTray = closeToTray;
            }
        }

        #endregion

        public SettingsViewModel()
        {
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

            SelectedFontFamilyItem = CurrentFontFamilyNameList[0];
            SelectVisibleIndex = 0;
        }

        public void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedFontFamily = CurrentFontFamilyList[SelectFontIndex];
        }
    }
}
