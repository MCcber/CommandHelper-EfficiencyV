using System.Collections.Generic;
using System.Windows.Documents;
using System;
using System.Windows;

namespace CBHK.Utility.Visual
{
    public static class ObfuscatedProvider
    {
        #region Field
        // 使用弱引用防止内存泄露
        private static readonly HashSet<WeakReference<Run>> RegisteredRuns = [];
        private static readonly Random _rng = new();
        private const string CharPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()+-=";
        #endregion

        #region Property
        // 附加属性用于标记
        public static readonly DependencyProperty IsObfuscatedProperty =
            DependencyProperty.RegisterAttached("IsObfuscated", typeof(bool), typeof(ObfuscatedProvider), new PropertyMetadata(false));

        public static bool GetIsObfuscated(DependencyObject obj) => (bool)obj.GetValue(IsObfuscatedProperty);
        public static void SetIsObfuscated(DependencyObject obj, bool value) => obj.SetValue(IsObfuscatedProperty, value);
        #endregion
    }
}