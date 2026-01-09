using CBHK.CustomControl;
using CBHK.Domain;
using CBHK.Domain.Model.Database;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.ViewModel.Common
{
    public partial class SettingsViewModel: ObservableObject
    {
        #region Field
        private InstalledFontCollection SystemFonts = new();
        private CBHKDataContext context;
        private EnvironmentConfig config;
        string fontListDirectory = AppDomain.CurrentDomain.BaseDirectory + "Resource\\Fonts";
        #endregion

        #region Property
        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _currentFontFamilyNameList = [];
        [ObservableProperty]
        private TextComboBoxItem _selectVisibleItem = null;

        [ObservableProperty]
        private int _selectFontIndex = 0;

        [ObservableProperty]
        private TextComboBoxItem _selectedFontFamilyItem;

        [ObservableProperty]
        private FontFamily _selectedFontFamily;

        List<FontFamily> CurrentFontFamilyList { get; set; } = [];

        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _stateList = [
                new() { Text = "Visible" },
                new() { Text = "Hidden" },
                new() { Text = "Collapsed" }
            ];

        [ObservableProperty]
        private bool closeToTray;

        #endregion

        #region Method
        public SettingsViewModel(CBHKDataContext Context)
        {
            #region Init
            context = Context;
            config = context.EnvironmentConfigSet.First();
            TextComboBoxItem visibilityItem = StateList.Where(item=>item.Text == config.Visibility).First();
            if(visibilityItem is not null)
            {
                SelectVisibleItem = visibilityItem;
            }
            PropertyChanged += SettingsViewModel_PropertyChanged;
            #endregion

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

            #region 设置默认字体
            CloseToTray = config.IsCloseToTray;
            SelectedFontFamilyItem = CurrentFontFamilyNameList[0];
            #endregion
        }

        private void SettingsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CloseToTray))
            {
                config.IsCloseToTray = CloseToTray;
            }
        }
        #endregion

        #region Event
        public void ViewState_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            config.Visibility = SelectVisibleItem.Text;
        }

        public void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedFontFamily = CurrentFontFamilyList[SelectFontIndex];
        }
        #endregion
    }
}