using CBHK.Common.Model;
using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Domain;
using CBHK.Domain.Model.Database;
using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
            context = Context;
            config = context.EnvironmentConfigSet.First();

            if (config is not null)
            {
                SelectedStateString = config.Visibility;
                SelectedVisualTypeString = config.VisualType;
                SelectedThemeTypeString = config.ThemeType;
                SelectedWindowCornerPreferenceTypeString = config.CornerPreferenceType;
            }
        }

        private async Task SwitchThemeWithAdornerAsync(Window window, string newThemeUri)
        {
            // A. 抓拍当前（旧）界面的快照
            var snapshot = CaptureWindowSnapshot(window);

            // B. 创建并挂载遮罩层
            var adorner = new ThemeTransitionAdorner(window.Content as UIElement, snapshot);
            var layer = AdornerLayer.GetAdornerLayer(window.Content as UIElement);
            layer.Add(adorner);

            // C. 【极其重要】强制让 UI 线程把这个遮罩层画出来
            // 如果不 await 这一步，ApplyNewTheme 会瞬间修改界面，导致快照还没盖上去界面就变了
            await Dispatcher.Yield(DispatcherPriority.Render);
            await Task.Delay(10);

            // D. 启动动画（不加 await），让圆圈开始扩散
            var animTask = adorner.PlayRevealAnimationAsync(TimeSpan.FromMilliseconds(500));

            // E. 此时底层换肤。虽然会有短暂卡顿，但因为上面盖着旧快照，用户看不见跳变
            ApplyNewTheme(newThemeUri);

            // F. 等待动画任务彻底完成（圆圈扩散到全屏）
            await animTask;
        }

        private static Brush CaptureWindowSnapshot(Window window)
        {
            // 1. 获取 DPI 缩放比例，保证在高 DPI 屏幕下截屏不模糊或错位
            var dpiX = 96.0;
            var dpiY = 96.0;
            var presentationSource = PresentationSource.FromVisual(window);
            if (presentationSource != null && presentationSource.CompositionTarget != null)
            {
                dpiX = 96.0 * presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = 96.0 * presentationSource.CompositionTarget.TransformToDevice.M22;
            }

            // 2. 计算实际的像素尺寸
            int pixelWidth = (int)(window.ActualWidth * (dpiX / 96.0));
            int pixelHeight = (int)(window.ActualHeight * (dpiY / 96.0));

            if (pixelWidth <= 0 || pixelHeight <= 0) return Brushes.Transparent;

            // 3. 渲染目标位图 (真正的静态快照)
            RenderTargetBitmap rtb = new(pixelWidth, pixelHeight, dpiX, dpiY, PixelFormats.Pbgra32);
            rtb.Render(window);

            // 4. 创建 ImageBrush
            var brush = new ImageBrush(rtb)
            {
                Stretch = Stretch.Fill, // 使用 Fill 铺满窗口
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            // 5. 冻结画刷以提升性能并切断与原 UI 的任何潜在联系
            brush.Freeze();

            return brush;
        }

        private static void ApplyNewTheme(string newThemeUri)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            // 1. 找出所有带有 "Theme" 关键字的字典（不分深浅，全部清除）
            var existingThemes = dictionaries
                .Where(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml"))
                .ToList();

            foreach (var theme in existingThemes)
            {
                dictionaries.Remove(theme);
            }

            // 2. 添加目标主题
            if (!string.IsNullOrEmpty(newThemeUri))
            {
                dictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri(newThemeUri, UriKind.RelativeOrAbsolute)
                });
            }
        }
        #endregion

        #region Event
        public void Settings_Loaded(object sender,RoutedEventArgs e)
        {
            PropertyChanged += SettingsViewModel_PropertyChanged;
            isLoaded = true;
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

        private async void SettingsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
                                await SwitchThemeWithAdornerAsync(targetView, SelectedThemeType.Text == "Light" ? Theme.LightTheme : Theme.DarkTheme);
                            }
                            break;
                        }
                    case "SelectedState":
                        {
                            config.Visibility = SelectedState.ToString();
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