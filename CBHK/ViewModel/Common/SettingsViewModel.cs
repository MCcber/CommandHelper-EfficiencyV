using CBHK.Common.Model;
using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Domain;
using CBHK.Domain.Model.Database;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CBHK.ViewModel.Common
{
    public partial class SettingsViewModel: ObservableObject
    {
        #region Field
        private bool isLoaded = false;
        private bool isPropertyChanging = false;
        private InstalledFontCollection SystemFonts = new();
        private CBHKDataContext context;
        private EnvironmentConfig config;
        string fontListDirectory = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Fonts";
        private VectorTextComboBox currentFontFamilyNameComboBox = null;
        #endregion

        #region Property
        [ObservableProperty]
        private VectorTextComboBoxItem selectedFontFamilyItem;

        List<FontFamily> CurrentFontFamilyList { get; set; } = [];

        [ObservableProperty]
        private VectorTextComboBoxItem selectedState;
        [ObservableProperty]
        private VectorTextComboBoxItem selectedVisualType;
        [ObservableProperty]
        private VectorTextComboBoxItem selectedThemeType;
        [ObservableProperty]
        private VectorTextComboBoxItem selectedWindowCornerPreferenceType;

        [ObservableProperty]
        private string selectedStateString;
        [ObservableProperty]
        private string selectedVisualTypeString;
        [ObservableProperty]
        private string selectedThemeTypeString;
        [ObservableProperty]
        private string selectedWindowCornerPreferenceTypeString;
        #endregion

        #region Method
        public SettingsViewModel(CBHKDataContext Context)
        {
            #region Init
            context = Context;
            config = context.EnvironmentConfigSet.First();
            #endregion
        }
        #endregion

        #region Event
        public void Settings_Loaded(object sender,RoutedEventArgs e)
        {
            PropertyChanged += SettingsViewModel_PropertyChanged;
            isLoaded = true;
            SelectedStateString = config.Visibility;
            SelectedThemeTypeString = config.ThemeType;
            SelectedVisualTypeString = config.VisualType;
            SelectedWindowCornerPreferenceTypeString = config.CornerPreferenceType;
        }

        public void FontFamilyComboBox_Loaded(object sender,RoutedEventArgs e)
        {
            #region Init
            if(sender is VectorTextComboBox comboBox)
            {
                currentFontFamilyNameComboBox = comboBox;
            }
            #endregion

            #region 获取自定义字体库
            string[] fontListFolder = Directory.GetFiles(fontListDirectory, "*ttf", SearchOption.AllDirectories);
            List<string> fontNameList = [];
            ObservableCollection<VectorTextComboBoxItem> currentFontFamilySource = [];
            foreach (string fontFile in fontListFolder)
            {
                FontFamily family = new(Path.GetFileNameWithoutExtension(fontFile));
                if (family is null)
                {
                    continue;
                }
                if (fontNameList.Count > 0 && fontNameList.Contains(family.FamilyNames.First().Value))
                    continue;
                fontNameList.Add(family.FamilyNames.First().Value);

                string currentText = new FontFamily(Path.GetFileNameWithoutExtension(fontFile)).FamilyNames.First().Value;


                if (!currentFontFamilySource.Any(item => item.Text == currentText))
                {
                    currentFontFamilySource.Add(new VectorTextComboBoxItem()
                    {
                        Text = currentText,
                        FontFamily = family
                    });
                    CurrentFontFamilyList.Add(family);
                }
            }
            #endregion

            #region 获取系统自带的字体库
            foreach (System.Drawing.FontFamily font in SystemFonts.Families)
            {
                if (font is null)
                {
                    continue;
                }
                FontFamily family = new(font.Name);
                CurrentFontFamilyList.Add(family);

                currentFontFamilySource.Add(new VectorTextComboBoxItem()
                {
                    FontFamily = family,
                    Text = font.Name
                });
            }
            currentFontFamilySource = [.. currentFontFamilySource.DistinctBy(item => item.Text)];
            currentFontFamilyNameComboBox.DataList = new(currentFontFamilySource);
            isLoaded = true;
            #endregion
        }

        private void SettingsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(!isLoaded || isPropertyChanging)
            {
                return;
            }
            if (e.PropertyName is not null && e.PropertyName.Length > 0)
            {
                switch (e.PropertyName)
                {
                    case "SelectedVisualType":
                        {
                            WindowVisualType windowVisualType = (WindowVisualType)Enum.Parse(typeof(WindowVisualType), SelectedVisualType.Text);
                            var targetView = Application.Current.Windows.OfType<VectorWindow>().FirstOrDefault(item=>item.GetType().ToString().EndsWith("MainView"));
                            if (targetView is not null)
                            {
                                isPropertyChanging = true;
                                targetView.VisualType = windowVisualType;
                                config.VisualType = SelectedVisualType.Text;
                            }
                            break;
                        }
                    case "SelectedThemeType":
                        {
                            WindowThemeType windowThemeType = (WindowThemeType)Enum.Parse(typeof(WindowThemeType), SelectedThemeType.Text);
                            var targetView = Application.Current.Windows.OfType<VectorWindow>().FirstOrDefault(item => item.GetType().ToString().EndsWith("MainView"));
                            if (targetView is not null)
                            {
                                isPropertyChanging = true;
                                targetView.ThemeType = windowThemeType;
                                config.ThemeType = SelectedThemeType.Text;
                            }
                            break;
                        }
                    case "SelectedState":
                        {
                            config.Visibility = SelectedState.Text;
                            break;
                        }
                    case "SelectedWindowCornerPreferenceType":
                        {
                            WindowCornerPreference preference = (WindowCornerPreference)Enum.Parse(typeof(WindowCornerPreference), SelectedWindowCornerPreferenceType.Text);
                            var targetView = Application.Current.Windows.OfType<VectorWindow>().FirstOrDefault(item => item.GetType().ToString().EndsWith("MainView"));
                            if (targetView is not null)
                            {
                                isPropertyChanging = true;
                                targetView.CornerPreference = preference;
                                config.CornerPreferenceType = preference.ToString();
                            }
                            break;
                        }
                }
                isPropertyChanging = false;
            }
        }
        #endregion
    }
}