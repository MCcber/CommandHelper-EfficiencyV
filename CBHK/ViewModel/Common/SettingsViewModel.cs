using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Domain;
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
using CBHK.Model.Data;
using CBHK.Domain.Model;
using System.ComponentModel;

namespace CBHK.ViewModel.Common
{
    public partial class SettingsViewModel: ObservableObject
    {
        #region Field
        private bool isLoaded = false;
        private bool isFontSourceLoaded = false;
        private bool isRestoringFont = false;
        private bool isInitializingCustomColors = false;
        private bool isPropertyChanging = false;
        private InstalledFontCollection SystemFonts = new();
        private EnvironmentSettingsService settingsService;
        private EnvironmentSettings config;
        string fontListDirectory = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Fonts";
        private VectorTextComboBox currentFontFamilyNameComboBox = null;
        #endregion

        #region Property
        [ObservableProperty]
        private VectorTextComboBoxItem selectedFontFamilyItem;

        [ObservableProperty]
        private Color customForeground = Colors.Black;

        [ObservableProperty]
        private Color customBackground = (Color)ColorConverter.ConvertFromString("#CC6B23");

        [ObservableProperty]
        private Color customWindowBackground = (Color)ColorConverter.ConvertFromString("#CC6B23");

        [ObservableProperty]
        private Visibility customColorPickerVisibility = Visibility.Collapsed;

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
        public SettingsViewModel(EnvironmentSettingsService SettingsService)
        {
            settingsService = SettingsService;
            config = settingsService.Current;

            if (config is not null)
            {
                SelectedStateString = config.Visibility;
                SelectedVisualTypeString = config.VisualType;
                SelectedThemeTypeString = config.ThemeType;
                SelectedWindowCornerPreferenceTypeString = config.CornerPreferenceType;

                // 自定义配色的初值来自设置（存的是 #AARRGGBB）。
                // 这里必须挡住回写：属性变更处理会把"前景+背景"两个值一起写回 config，
                // 先赋前景时背景还是默认值，于是 config.CustomBackground 被默认值冲掉，
                // 紧接着读背景就读不到了——表现为前景正常、背景永远是默认色。
                isInitializingCustomColors = true;
                try
                {
                    if (ThemeManager.TryParseColor(config.CustomForeground, out Color initialForeground))
                    {
                        CustomForeground = initialForeground;
                    }
                    if (ThemeManager.TryParseColor(config.CustomBackground, out Color initialBackground))
                    {
                        CustomBackground = initialBackground;
                    }
                    if (ThemeManager.TryParseColor(config.CustomWindowBackground, out Color initialWindowBackground))
                    {
                        CustomWindowBackground = initialWindowBackground;
                    }
                }
                finally
                {
                    isInitializingCustomColors = false;
                }
            }
        }

        private static async Task SwitchThemeWithAdornerAsync(Window window, string newThemeUri)
        {
            //抓拍当前（旧）界面的快照
            var snapshot = CaptureWindowSnapshot(window);

            //创建并挂载遮罩层
            var adorner = new ThemeTransitionAdorner(window.Content as UIElement, snapshot);
            var layer = AdornerLayer.GetAdornerLayer(window.Content as UIElement);
            layer.Add(adorner);

            //强制让 UI 线程把这个遮罩层画出来
            // 如果不 await 这一步，ApplyNewTheme 会瞬间修改界面，导致快照还没盖上去界面就变了
            await Dispatcher.Yield(DispatcherPriority.Render);
            await Task.Delay(10);

            //启动动画（不加 await），让圆圈开始扩散
            var animTask = adorner.PlayRevealAnimationAsync(TimeSpan.FromMilliseconds(500));

            //此时底层换肤。虽然会有短暂卡顿，但因为上面盖着旧快照，用户看不见跳变
            ThemeManager.ApplyNewTheme(newThemeUri);

            //等待动画任务彻底完成（圆圈扩散到全屏）
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
                        ItemID = currentText,
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
                    ItemID = font.Name,
                    FontFamily = family,
                    Text = font.Name
                });
            }
            currentFontFamilySource = [.. currentFontFamilySource.DistinctBy(item => item.Text)];
            // 从这里一直挡住写回，直到延后回显跑完：
            // 换 ItemsSource 之后控件自己那轮 Loaded 会再包一层 CollectionView，
            // ComboBox 在这个窗口里可能把选中项推成第一项，那不是用户的选择。
            isRestoringFont = true;
            currentFontFamilyNameComboBox.ItemsSource = currentFontFamilySource;
            // 设置初始选中（替代 XAML 中的 SelectedIndex="0"，避免与 SelectedItem 双向绑定冲突）
            isFontSourceLoaded = true;
            isLoaded = true;

            // 回显必须延后到"控件自己那轮 Loaded 处理完"之后：
            // VectorTextComboBox 会在它自己的 Loaded 里把 ItemsSource 再包一层 CollectionView 并重新赋值，
            // 那一步会把刚设好的 SelectedItem 再清一次（新集合、新视图里都没有同一个对象）。
            // 在这里同步回显等于"设完立刻被清掉"，界面上就是回到第一项。
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    RestoreSelectedFont();
                }
                finally
                {
                    isRestoringFont = false;
                }
            }), DispatcherPriority.Loaded);
            #endregion
        }

        /// <summary>
        /// 按已保存的字体名回显选中项。用 ComboBox 当前的 Items（最外层视图）而不是本地集合，
        /// 保证赋给 SelectedItem 的对象确实在列表里。期间 isRestoringFont 挡住写回：
        /// 匹配不到会退回第一项，但这个退回动作绝不能被存成用户选择。
        /// </summary>
        private void RestoreSelectedFont()
        {
            if (currentFontFamilyNameComboBox is null)
            {
                return;
            }

            List<VectorTextComboBoxItem> items = [.. currentFontFamilyNameComboBox.Items.OfType<VectorTextComboBoxItem>()];
            if (items.Count == 0)
            {
                return;
            }

            string desiredFont = config?.FontFamily;
            VectorTextComboBoxItem matchedItem = items.FirstOrDefault(item =>
                    string.Equals(item.FontFamily?.Source, desiredFont, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.Text, desiredFont, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.ItemID, desiredFont, StringComparison.OrdinalIgnoreCase))
                ?? items[0];

            // item 重写了 Equals（按 ItemID 比），[ObservableProperty] 的相等判断会把
            // "内容相同但不是同一个对象"的赋值吞掉，ComboBox 会继续持有旧集合里的对象。先清空再赋。
            SelectedFontFamilyItem = null;
            SelectedFontFamilyItem = matchedItem;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            switch (e.PropertyName)
            {
                case "SelectedFontFamilyItem":
                    {
                        // 这里原来写的是 Application.Current.Resources.Contains("GlobalFont")。
                        // ResourceDictionary.Contains 只查字典自己的条目、**不搜 MergedDictionaries**，
                        // 而 GlobalFont 定义在 App.xaml 并入的 Visual.xaml 里 —— 判断恒为 false，
                        // 这段赋值从来没执行过，字体自然"设了没反应"。
                        // 还要等下拉源装好再接手：初始化时会自动选中第一项，
                        // 不拦住的话一打开设置页就会把全局字体改成列表第一项。
                        if (isFontSourceLoaded && !isRestoringFont && SelectedFontFamilyItem?.FontFamily is FontFamily family)
                        {
                            Application.Current.Resources["GlobalFont"] = family;
                            // 落盘：下次启动由 App.OnStartup 还原到 GlobalFont
                            config.FontFamily = family.Source;
                        }
                        break;
                    }
                case "SelectedThemeType":
                    {
                        if (SelectedThemeType.Text == "Custom")
                        {
                            CustomColorPickerVisibility = Visibility.Visible;
                        }
                        else
                        {
                            CustomColorPickerVisibility = Visibility.Collapsed;
                        }
                        break;
                    }
                case "CustomForeground":
                case "CustomBackground":
                    {
                        // 写进 CustomTheme.xaml 那一份合并字典，不写顶层：
                        // 顶层条目会遮蔽所有主题字典，切回预制配色时画刷就回不去了。
                        ThemeManager.ApplyCustomColors(CustomForeground, CustomBackground);
                        if (!isInitializingCustomColors)
                        {
                            // 记进设置，关窗时随其它设置一起写回 EnvironmentSettings.json。
                            // 初始化阶段不能写：此时另一个颜色还没从设置里读出来，会把它冲成默认值。
                            config.CustomForeground = CustomForeground.ToString();
                            config.CustomBackground = CustomBackground.ToString();
                        }
                        break;
                    }
                case "CustomWindowBackground":
                    {
                        ThemeManager.ApplyCustomWindowBackground(CustomWindowBackground);
                        if (!isInitializingCustomColors)
                        {
                            config.CustomWindowBackground = CustomWindowBackground.ToString();
                        }
                        break;
                    }
            }
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
                                config.ThemeType = windowThemeType.ToString();
                                string themePath = windowThemeType switch
                                {
                                    WindowThemeType.CommandBlockOrange => Theme.CommandBlockOrangeTheme,
                                    WindowThemeType.CommandBlockBlueGreen => Theme.CommandBlockBlueGreenTheme,
                                    WindowThemeType.CommandBlockPurple => Theme.CommandBlockPurpleTheme,
                                    _ => Theme.CustomTheme
                                };
                                await SwitchThemeWithAdornerAsync(targetView, themePath);
                                CustomColorPickerVisibility = Visibility.Collapsed;
                                if (windowThemeType is WindowThemeType.Custom)
                                {
                                    CustomColorPickerVisibility = Visibility.Visible;

                                    // 切到 Custom 一律以"上次存下来的颜色"为准：把设置里的值读回两个取色器
                                    // （换主题会新建一份 CustomTheme.xaml，xaml 里只有默认色，必须重新写一遍）
                                    if (ThemeManager.TryParseColor(config.CustomForeground, out Color customForeground))
                                    {
                                        CustomForeground = customForeground;
                                    }
                                    if (ThemeManager.TryParseColor(config.CustomBackground, out Color customBackground))
                                    {
                                        CustomBackground = customBackground;
                                    }

                                    ThemeManager.ApplyCustomColors(CustomForeground, CustomBackground);
                                }
                            }
                            break;
                        }
                    case "SelectedState":
                        {
                            // 原来写的是 SelectedState.ToString() —— 那是个 VectorTextComboBoxItem，
                            // ToString() 得到的是类型全名，落盘后 Visibility 就变成
                            // "CBHK.CustomControl.VectorComboBox.VectorTextComboBoxItem"，窗口状态永远读不回来。
                            config.Visibility = SelectedState?.Text;
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