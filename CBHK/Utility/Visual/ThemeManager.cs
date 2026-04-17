using System;
using System.Linq;
using System.Windows;

namespace CBHK.Utility.Visual
{
    public static class ThemeManager
    {
        public static void ApplyNewTheme(string newThemeUri)
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
    }
}
