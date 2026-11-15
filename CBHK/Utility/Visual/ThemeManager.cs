using CBHK.Model.Constant;
using CBHK.Model.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    public static class ThemeManager
    {
        #region Event
        /// <summary>
        /// 主题资源被替换或改写后触发（切主题、改自定义配色）。
        /// 订阅方据此重算那些"在代码里按主题画刷算出来的本地颜色"（例如主页骨架屏）。
        /// </summary>
        public static event Action ThemeChanged;

        // 订阅方在回调里可能又去改资源（例如重新写自定义配色），不挡的话会无限递归
        private static bool isNotifyingThemeChanged;

        private static void NotifyThemeChanged()
        {
            if (isNotifyingThemeChanged)
            {
                return;
            }

            isNotifyingThemeChanged = true;
            try
            {
                ThemeChanged?.Invoke();
            }
            finally
            {
                isNotifyingThemeChanged = false;
            }
        }
        #endregion

        #region Method
        /// <summary>用户选定的窗体底色。只在 Default 特效下真正写进资源。</summary>
        public static Color CustomWindowBackground { get; private set; } = Colors.Transparent;

        private static WindowVisualType currentVisualType = WindowVisualType.Default;

        /// <summary>当前已合并的主题字典 URI，用于避免同一个主题被重复重建。</summary>
        private static string currentThemeUri;

        /// <summary>
        /// 记录用户选的窗体底色，并按当前特效立刻刷新资源。
        /// 非 Default 特效时资源会是 Transparent——否则这层底色会盖住 Mica/Acrylic 的系统 backdrop。
        /// </summary>
        public static bool ApplyCustomWindowBackground(Color customWindowBackground)
        {
            CustomWindowBackground = customWindowBackground;
            return RefreshWindowBackground();
        }

        /// <summary>窗体特效变化时调用：Default 用用户选的颜色，其余特效一律透明，让系统材质透出来。</summary>
        public static bool ApplyWindowBackgroundForVisualType(WindowVisualType visualType)
        {
            currentVisualType = visualType;
            return RefreshWindowBackground();
        }

        private static bool RefreshWindowBackground()
        {
            Color color = currentVisualType == WindowVisualType.Default ? CustomWindowBackground : Colors.Transparent;

            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (dictionary.Source is null || dictionary.Source.OriginalString != Theme.VisualFileName)
                {
                    continue;
                }

                dictionary[Theme.CustomWindowBackgroundBrushKey] = new SolidColorBrush(color);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 把自定义配色写进**当前合并着的** CustomTheme.xaml 里那两个画刷，而不是写顶层资源。
        /// 顶层条目会遮蔽所有主题字典，切回预制主题时画刷就回不去了；
        /// 写进字典里则跟着字典一起被换掉。没切到 Custom 主题时它不在合并列表里，直接 no-op。
        /// </summary>
        /// <returns>是否找到了 Custom 主题字典。</returns>
        public static bool ApplyCustomColors(Color foreground, Color background)
        {
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (dictionary.Source is null || !dictionary.Source.OriginalString.Contains(Theme.CustomTheme))
                {
                    continue;
                }

                dictionary[Theme.CommonForeground] = new SolidColorBrush(foreground);
                dictionary[Theme.CommonBackground] = new SolidColorBrush(background);
                NotifyThemeChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 按十六进制字符串应用自定义配色（启动时从设置里读出来的那两个值）。
        /// 解析失败或当前没合并 Custom 主题时返回 false，不抛。
        /// </summary>
        public static bool TryApplyCustomColors(string foregroundHex, string backgroundHex)
        {
            if (!TryParseColor(foregroundHex, out Color foreground) || !TryParseColor(backgroundHex, out Color background))
            {
                return false;
            }

            return ApplyCustomColors(foreground, background);
        }

        /// <summary>#AARRGGBB / #RRGGBB → Color；空串或非法值返回 false。</summary>
        public static bool TryParseColor(string hex, out Color color)
        {
            color = default;

            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            try
            {
                color = (Color)ColorConverter.ConvertFromString(hex);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ApplyNewTheme(string newThemeUri)
        {
            // 同一个主题不再重建字典。子窗体激活时会把主窗体的 ThemeType 抄一遍（VectorWindow_Activated），
            // 每次都重建的话，写进 CustomTheme.xaml 的自定义配色会被新字典的 xaml 默认值冲掉——
            // 表现就是"打开子窗体背景退回预设色，焦点回主窗才恢复"。
            if (!string.IsNullOrEmpty(newThemeUri) && string.Equals(currentThemeUri, newThemeUri, StringComparison.Ordinal))
            {
                return;
            }

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

            currentThemeUri = newThemeUri;
            NotifyThemeChanged();
        } 
        #endregion
    }
}
