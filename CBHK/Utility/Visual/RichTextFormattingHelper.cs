using CBHK.Model.Common;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    public class RichTextFormattingHelper
    {
        #region Method
        public static void ToggleBold(RichTextBox richTextBox)
        {
            ToggleProperty(richTextBox,
                TextElement.FontWeightProperty,
                FontWeights.Bold,
                FontWeights.Normal);
        }

        public static void ToggleItalic(RichTextBox richTextBox)
        {
            ToggleProperty(richTextBox,
                TextElement.FontStyleProperty,
                FontStyles.Italic,
                FontStyles.Normal);
        }

        public static void ToggleUnderline(RichTextBox richTextBox)
        {
            var selection = richTextBox.Selection;
            if (selection.IsEmpty) return;

            // 获取当前的文本装饰
            var currentDecorations = selection.GetPropertyValue(Inline.TextDecorationsProperty);

            if (currentDecorations == DependencyProperty.UnsetValue)
            {
                // 混合状态：添加下划线
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
            }
            else if (currentDecorations is TextDecorationCollection decorations)
            {
                // 检查是否已包含下划线
                bool hasUnderline = HasUnderline(decorations);
                TextDecorationCollection textDecorations = [];

                if (hasUnderline)
                {
                    // 移除下划线，但保留其他装饰
                    textDecorations = RemoveUnderline(decorations);
                }
                else
                {
                    // 添加下划线到现有装饰集合
                    textDecorations = AddUnderline(decorations);
                }
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, textDecorations);
            }
            else
            {
                // 当前没有装饰：添加下划线
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
            }
        }

        public static void ToggleStrikethrough(RichTextBox richTextBox)
        {
            var selection = richTextBox.Selection;
            if (selection.IsEmpty) return;

            // 获取当前的文本装饰
            var currentDecorations = selection.GetPropertyValue(Inline.TextDecorationsProperty);

            if (currentDecorations == DependencyProperty.UnsetValue)
            {
                // 混合状态：添加删除线
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough);
            }
            else if (currentDecorations is TextDecorationCollection decorations)
            {
                // 检查是否已包含删除线
                bool hasStrikethrough = HasStrikethrough(decorations);
                TextDecorationCollection textDecorations = [];

                if (hasStrikethrough)
                {
                    // 移除删除线，但保留其他装饰
                    textDecorations = RemoveStrikethrough(decorations);
                }
                else
                {
                    // 添加删除线到现有装饰集合
                    textDecorations = AddStrikethrough(decorations);
                }
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, textDecorations);
            }
            else
            {
                // 当前没有装饰：添加删除线
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough);
            }
        }

        public static void ToggleObfuscated(RichTextBox richTextBox)
         {
            var selection = richTextBox.Selection;
            if (selection.IsEmpty) return;

            // 1. 手动检测当前选区状态（以选区起点所在的 Run 为准，或遍历检查）
            bool? currentState = GetCustomPropertyState(selection, ObfuscatedProvider.IsObfuscatedProperty);
            bool targetState = !(currentState ?? false); // 如果是混合状态或 false，则开启

            // 2. 配合之前的“GPU方案”：同步设置透明度和字体
            if (targetState)
            {
                selection.ApplyPropertyValue(TextElement.ForegroundProperty, richTextBox.Background);
                selection.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Consolas"));
            }
            else
            {
                // 这里建议恢复编辑器的默认前景色
                selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
                selection.ApplyPropertyValue(TextElement.FontFamilyProperty, richTextBox.FontFamily); // 尝试清除或设回默认
            }

            // 3. 遍历选区并应用属性到每一个 Run
            ApplyCustomPropertyToSelection(selection, ObfuscatedProvider.IsObfuscatedProperty, targetState);
        }

        public static bool? GetCustomPropertyState(TextSelection selection, DependencyProperty property)
        {
            if (selection.IsEmpty)
            {
                // 检查光标所在位置的 Run
                return selection.Start.Parent is Run run && (bool)run.GetValue(property);
            }

            // 检查选区开始处的 Run
            // 注意：如果选区跨越了多个不同状态的 Run，这里简单返回第一个的状态
            // 如果要更严谨，可以遍历选区检查是否所有 Run 的值都一致
            TextPointer navigator = selection.Start;
            bool? firstValue = null;

            while (navigator != null && navigator.CompareTo(selection.End) < 0)
            {
                if (navigator.Parent is Run run)
                {
                    bool val = (bool)run.GetValue(property);
                    if (firstValue == null) firstValue = val;
                    else if (firstValue != val) return null; // 混合状态

                    navigator = run.ElementEnd;
                }
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }

            return firstValue ?? false;
        }

        public static void ApplyCustomPropertyToSelection(TextRange selection, DependencyProperty property, object value)
        {
            // 1. 获取当前状态并强行应用一次标准属性触发拆分
            selection.ApplyPropertyValue(TextElement.FontSizeProperty, selection.GetPropertyValue(TextElement.FontSizeProperty));

            // 2. 物理遍历这两个位置之间的所有相邻元素
            TextPointer navigator = selection.Start;
            while (navigator != null && navigator.CompareTo(selection.End) < 0)
            {
                // 检查当前指针所在的父级或后续元素
                if (navigator.Parent is Run run)
                {
                    run.SetValue(property, value);
                    // 处理完这个 Run，直接跳到它的末尾之后
                    navigator = run.ElementEnd.GetNextContextPosition(LogicalDirection.Forward);
                    continue;
                }

                // 如果还没进到 Run 里，就继续往后找
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        private static void ToggleProperty(RichTextBox richTextBox,
            DependencyProperty property,
            object activeValue,
            object defaultValue)
        {
            var selection = richTextBox.Selection;
            if (selection.IsEmpty) return;

            var currentValue = selection.GetPropertyValue(property);

            // 判断逻辑：
            // 1. 如果选中区域是混合状态（UnsetValue）→ 应用目标样式
            // 2. 如果已经是目标样式 → 取消样式
            // 3. 否则（不是目标样式）→ 应用目标样式
            object newValue = currentValue == DependencyProperty.UnsetValue ||
                              !currentValue.Equals(activeValue)
                ? activeValue
                : defaultValue;

            selection.ApplyPropertyValue(property, newValue);
        }

        // 检查当前选中区域的样式状态
        public static FormattingState GetFormattingState(RichTextBox richTextBox)
        {
            var selection = richTextBox.Selection;
            if (selection.IsEmpty)
            {
                return FormattingState.None;
            }

            return new FormattingState
            {
                Bold = GetPropertyState(selection,
                    TextElement.FontWeightProperty,
                    FontWeights.Bold),
                Italic = GetPropertyState(selection,
                    TextElement.FontStyleProperty,
                    FontStyles.Italic),
                Obfuscated = GetCustomPropertyState(selection, ObfuscatedProvider.IsObfuscatedProperty)
            };
        }

        public static bool? GetPropertyState(TextSelection selection,
            DependencyProperty property,
            object activeValue)
        {
            var value = selection.GetPropertyValue(property);

            if (value == DependencyProperty.UnsetValue)
                return null; // null 表示混合状态
            else
                return value.Equals(activeValue);
        }

        /// <summary>
        /// 检查文本装饰集合是否包含下划线
        /// </summary>
        /// <param name="decorations"></param>
        /// <returns></returns>
        private static bool HasUnderline(TextDecorationCollection decorations)
        {
            if (decorations == null) return false;

            // 查找任何位置为Underline的装饰
            return decorations.Any(d => d.Location == TextDecorationLocation.Underline);
        }

        /// <summary>
        /// 检查文本装饰集合是否包含删除线
        /// </summary>
        /// <param name="decorations"></param>
        /// <returns></returns>
        private static bool HasStrikethrough(TextDecorationCollection decorations)
        {
            if (decorations == null) return false;

            // 查找任何位置为Strikethrough的装饰
            return decorations.Any(d => d.Location == TextDecorationLocation.Strikethrough);
        }

        /// <summary>
        /// 从装饰集合中移除下划线
        /// </summary>
        /// <param name="decorations"></param>
        /// <returns></returns>
        private static TextDecorationCollection RemoveUnderline(TextDecorationCollection decorations)
        {
            if (decorations == null) return null;

            // 创建新的装饰集合，排除下划线
            var newDecorations = new TextDecorationCollection();

            foreach (var decoration in decorations)
            {
                if (decoration.Location != TextDecorationLocation.Underline)
                {
                    newDecorations.Add(decoration);
                }
            }

            // 如果移除下划线后集合为空，返回null
            return newDecorations.Count > 0 ? newDecorations : null;
        }

        /// <summary>
        /// 从装饰集合中移除删除线
        /// </summary>
        /// <param name="decorations"></param>
        /// <returns></returns>
        private static TextDecorationCollection RemoveStrikethrough(TextDecorationCollection decorations)
        {
            if (decorations == null) return null;

            // 创建新的装饰集合，排除删除线
            var newDecorations = new TextDecorationCollection();

            foreach (var decoration in decorations)
            {
                if (decoration.Location is not TextDecorationLocation.Strikethrough)
                {
                    newDecorations.Add(decoration);
                }
            }

            // 如果移除删除线后集合为空，返回null
            return newDecorations.Count > 0 ? newDecorations : null;
        }

        /// <summary>
        /// 向装饰集合中添加下划线
        /// </summary>
        /// <param name="decorations"></param>
        /// <returns></returns>
        private static TextDecorationCollection AddUnderline(TextDecorationCollection decorations)
        {
            var newDecorations = new TextDecorationCollection();

            // 复制原有的装饰
            if (decorations != null)
            {
                foreach (var decoration in decorations)
                {
                    newDecorations.Add(decoration);
                }
            }

            // 添加下划线（如果还没有）
            if (!HasUnderline(newDecorations))
            {
                var underline = new TextDecoration
                {
                    Location = TextDecorationLocation.Underline,
                    Pen = new Pen(Brushes.Black, 1)
                };
                newDecorations.Add(underline);
            }

            return newDecorations;
        }

        /// <summary>
        /// 向装饰集合中添加删除线
        /// </summary>
        /// <param name="decorations"></param>
        /// <returns></returns>
        private static TextDecorationCollection AddStrikethrough(TextDecorationCollection decorations)
        {
            var newDecorations = new TextDecorationCollection();

            // 复制原有的装饰
            if (decorations != null)
            {
                foreach (var decoration in decorations)
                {
                    newDecorations.Add(decoration);
                }
            }

            // 添加删除线（如果还没有）
            if (!HasStrikethrough(newDecorations))
            {
                var strikethrough = new TextDecoration
                {
                    Location = TextDecorationLocation.Strikethrough,
                    Pen = new Pen(Brushes.Black, 1)
                };
                newDecorations.Add(strikethrough);
            }

            return newDecorations;
        }

        /// <summary>
        /// 获取下划线状态（支持三种状态）
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        public static bool? GetUnderlineState(TextSelection selection)
        {
            if (selection.IsEmpty) return false;

            var decorations = selection.GetPropertyValue(Inline.TextDecorationsProperty);

            if (decorations == DependencyProperty.UnsetValue)
                return null; // 混合状态

            if (decorations is TextDecorationCollection decorationCollection)
                return HasUnderline(decorationCollection);

            return false;
        }

        /// <summary>
        /// 获取删除线状态（支持三种状态）
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        public static bool? GetStrikethroughState(TextSelection selection)
        {
            if (selection.IsEmpty) return false;

            var decorations = selection.GetPropertyValue(Inline.TextDecorationsProperty);

            if (decorations == DependencyProperty.UnsetValue)
                return null; // 混合状态

            if (decorations is TextDecorationCollection decorationCollection)
                return HasStrikethrough(decorationCollection);

            return false;
        }
        #endregion
    }
}
